# SecondBlock.Products.API

Web API RESTful de gestión de productos en .NET 10, preparada para Visual Studio 2026 Community. Implementa tres capas, DTOs, validaciones con Data Annotations y almacenamiento en memoria.

La solución incluye el CRUD de `/api/products`, Swagger UI para probarlo desde el navegador, mappers estáticos, un helper de validación y tests xUnit de las tres capas.

Accesos rápidos: [requisitos](#requisitos), [clonar y compilar](#clonar-y-compilar), [ejecutar y probar la API](#ejecutar-y-probar-la-api), [tests](#compilar-y-ejecutar-tests), [arquitectura](#capas-y-responsabilidades).

## Requisitos

- Git para clonar el repositorio.
- SDK de **.NET 10**. Comprobar la instalación con `dotnet --version`; debe estar disponible un SDK `10.x`.
- Para ejecutar desde el IDE: **Visual Studio 2026 Community** con la carga de trabajo **ASP.NET y desarrollo web** y soporte para .NET 10.
- Conexión a Internet durante la restauración inicial de paquetes NuGet.
- PowerShell en Windows para ejecutar el script de comprobaciones HTTP.

El almacenamiento es en memoria. La aplicación se puede ejecutar directamente después de restaurar y compilar; no requiere configurar una base de datos, migraciones, credenciales ni servicios externos.

## Clonar y compilar

1. Abrir una terminal en la carpeta donde se quiera guardar el proyecto.
2. Clonar el repositorio y entrar en su carpeta:

   ```powershell
   git clone https://github.com/MarcosArielFontenla/products-block-2-api-devlights.git
   cd products-block-2-api-devlights
   ```

3. Restaurar dependencias y compilar la solución:

   ```powershell
   dotnet restore SecondBlock.Products.API.slnx
   dotnet build SecondBlock.Products.API.slnx --no-restore
   ```

4. Seguir los pasos de [ejecución](#ejecutar-y-probar-la-api) para iniciar la API, o ejecutar las [pruebas automatizadas](#compilar-y-ejecutar-tests).

Todos los comandos del README se ejecutan desde la raíz del repositorio, donde se encuentra `SecondBlock.Products.API.slnx`.

## Estructura

```text
SecondBlock.Products.API.slnx
src/
  SecondBlock.Products.API/             # Controllers, configuración HTTP y DI
    Controllers/ProductsController.cs
  SecondBlock.Products.Business/        # DTOs y servicios de negocio
    DTOs/CreateProductDto.cs
    DTOs/ProductDto.cs
    Helpers/ValidationHelper.cs
    Mappers/ProductMapper.cs
    Services/Interfaces/IProductService.cs
    Services/ProductService.cs
  SecondBlock.Products.DataAccess/      # Entidad y repositorio en memoria
    Entities/Product.cs
    Repositories/Interfaces/IProductRepository.cs
    Repositories/InMemoryProductRepository.cs
tests/
  SecondBlock.Products.API.UnitTests/        # Respuestas de los controladores
    ProductsControllerTests.cs
  SecondBlock.Products.Business.UnitTests/   # Servicios y validaciones de DTOs
    CreateProductDtoTests.cs
    ProductServiceTests.cs
  SecondBlock.Products.DataAccess.UnitTests/ # CRUD y persistencia entre instancias
    InMemoryProductRepositoryTests.cs
  Verify-Api.ps1
```

Todos los proyectos de producción, tanto la Web API como las próximas bibliotecas o capas, deben crearse en `src/`. Los proyectos de tests unitarios deben crearse en `tests/`, usar xUnit y llamarse `<ProjectName>.UnitTests`.

La solución agrupa los proyectos en las carpetas `src` y `tests` del Explorador de soluciones, que corresponden a las carpetas físicas del repositorio.

## Convenciones de nombres

Los nombres de archivos, tipos, miembros, variables y tests están en inglés. Los comentarios, mensajes de validación y ejemplos de código también usan inglés; la documentación del proyecto se mantiene en español.

| Elemento | Convención | Ejemplos |
| --- | --- | --- |
| Clases, métodos y propiedades | `PascalCase` | `ProductService`, `GetById`, `IsActive` |
| Interfaces | Prefijo `I` y `PascalCase` | `IProductService`, `IProductRepository` |
| DTOs | Sufijo `Dto` | `CreateProductDto`, `ProductDto` |
| Variables locales y parámetros | `camelCase` | `product`, `productService`, `createdProduct` |
| Campos privados de instancia | Prefijo `_` y `camelCase` | `_productService`, `_productRepository` |
| Campos privados estáticos | Prefijo `s_` y `camelCase` | `s_products`, `s_nextId` |
| Archivos C# | Mismo nombre que el tipo principal | `ProductsController.cs`, `InMemoryProductRepository.cs` |
| Tests | `Method_Scenario_ExpectedBehavior` | `GetById_MissingProduct_ReturnsNotFound` |
| Propiedades JSON | `camelCase` | `name`, `price`, `category`, `isActive` |

Las operaciones CRUD se llaman `GetAll`, `GetById`, `Create`, `Update` y `Delete` en las tres capas. Estas reglas también están registradas en `AGENTS.md` para el código futuro.

`GetAll()` devuelve `List<Product>` en el repositorio y `List<ProductDto>` en el servicio. Las interfaces, implementaciones, dobles de prueba y el tipo de respuesta declarado para Swagger usan esos mismos tipos. El repositorio usa `ToList()` para crear una lista de resultados independiente y maneja las entidades por referencia. El servicio recorre esa lista con `foreach`, mapea cada producto y agrega el DTO a una nueva lista con `Add`.

La inyección de dependencias usa constructores tradicionales explícitos y campos `private readonly` en las clases que reciben dependencias, como el controlador y el servicio:

```csharp
private readonly IProductRepository _productRepository;

public ProductService(IProductRepository productRepository)
{
    _productRepository = productRepository;
}

public bool Delete(int id)
{
    return _productRepository.Delete(id);
}
```

El repositorio actual administra directamente los productos en memoria, por lo que se instancia sin parámetros. Cuando incorpore una dependencia real, su constructor seguirá el mismo formato.

Los métodos y constructores usan cuerpos con llaves `{ }` y `return` cuando devuelven un valor, incluso cuando contienen una sola instrucción. Las lambdas se usan para predicados, LINQ y callbacks. Esta convención también se aplica a los tests y sus dobles de prueba. `.editorconfig` configura las preferencias de Visual Studio para mantener este estilo.

Los servicios, mappers y helpers usan un estilo sencillo y tradicional: condiciones con `if`, recorridos con `foreach`, variables intermedias y asignaciones de propiedades paso a paso. Las comprobaciones de nulos se escriben con `if` y excepciones explícitas. El servicio conserva la DI por constructor y los mappers y helpers permanecen como clases estáticas sin estado propio.

Por ejemplo, el servicio prepara la lista de respuesta de esta forma:

```csharp
public List<ProductDto> GetAll()
{
    var products = _productRepository.GetAll();
    var productDtos = new List<ProductDto>();

    foreach (var product in products)
    {
        var productDto = ProductMapper.ToDto(product);
        productDtos.Add(productDto);
    }

    return productDtos;
}
```

## Capas y responsabilidades

El flujo de cada petición es `ProductsController -> IProductService -> IProductRepository`.

- **API:** expone `/api/products`, recibe DTOs y devuelve `IActionResult`. Usa `[ApiController]` para responder automáticamente `400 Bad Request` con `ValidationProblemDetails` cuando el body no es válido.
- **Business:** contiene los DTOs y `ProductService`, que coordina las operaciones de negocio mediante `IProductRepository`, `ValidationHelper` y `ProductMapper`. Los endpoints devuelven `ProductDto`.
- **DataAccess:** contiene la entidad `Product`, `IProductRepository` e `InMemoryProductRepository`. El repositorio administra una `List<Product>` estática privada y un contador estático para asignar IDs.

La API referencia a Business y a DataAccess para registrar las dependencias. Business referencia a DataAccess. DataAccess no depende de las capas superiores.

`Program.cs` registra `IProductService` con `ProductService` e `IProductRepository` con `InMemoryProductRepository`, ambos mediante `AddScoped`. Cada petición crea su servicio y repositorio. La lista y el contador de IDs son `static`, por lo que sus datos se conservan entre las distintas instancias del repositorio mientras la aplicación está ejecutándose.

Los datos empiezan vacíos y se pierden al reiniciar la aplicación. Los IDs se generan automáticamente y no se reutilizan al eliminar un producto.

El repositorio usa operaciones tradicionales: recorre la lista para buscar por ID, agrega productos, modifica las propiedades de un producto existente y elimina el producto encontrado. Esta versión didáctica no sincroniza accesos simultáneos ni clona entidades; está orientada a practicar el CRUD en memoria con peticiones secuenciales.

## Mappers y helpers

El mapeo entre `Product` y los DTOs pertenece a **Business**, la capa que conoce ambos tipos. Cada entidad tiene su mapper específico dentro de `Mappers/`, por ejemplo `ProductMapper`, para reutilizar la conversión desde distintos servicios.

| Método de `ProductMapper` | Responsabilidad |
| --- | --- |
| `ToDto(Product product)` | Crear un `ProductDto` independiente con todos los datos de la entidad |
| `ToEntity(CreateProductDto dto)` | Crear una entidad con los campos del DTO; conservar los valores iniciales del modelo para `Id` e `IsActive` |
| `UpdateEntity(CreateProductDto dto, Product product)` | Aplicar los campos editables a una entidad existente, conservando su `Id` e `IsActive` |

El servicio valida el DTO antes de llamar al mapper. El mapper transforma los datos con asignaciones explícitas de propiedades y comprueba que el objeto y los campos necesarios estén presentes antes de aplicarlos. El servicio coordina las reglas de negocio y el repositorio administra el almacenamiento. Estos mappers son clases estáticas sin estado propio, con métodos con llaves, que pueden usarse directamente sin registrarlas en el contenedor de DI.

Los **helpers se ubican según su responsabilidad y dependencias**:

| Tipo de utilidad | Ubicación |
| --- | --- |
| Validación de DTOs con Data Annotations | `Business/Helpers/ValidationHelper.cs` |
| Utilidades relacionadas con HTTP o respuestas de la API | `API/Helpers/`, cuando sean necesarias |
| Utilidades relacionadas con almacenamiento | `DataAccess/Helpers/`, cuando sean necesarias |
| Utilidades genéricas usadas por varios proyectos | Un proyecto común, cuando exista esa necesidad, sin dependencias hacia las capas de la aplicación |

`ValidationHelper.Validate(object instance)` comprueba con un `if` que el objeto no sea nulo, crea un `ValidationContext` en una variable y llama a `Validator.ValidateObject` para comprobar todas sus Data Annotations. Lanza `ValidationException` si el objeto es inválido. `ProductService` lo usa en creación y edición, también al invocarse fuera de HTTP. Su ubicación actual permite reutilizarlo con otros DTOs de Business. La API mantiene la validación automática de `[ApiController]` para responder `400 Bad Request`.

Los helpers se crean para necesidades concretas y sus nombres deben expresar su función. Las carpetas `Mappers/` y `Helpers/` organizan responsabilidades dentro de su capa.

## Contrato HTTP

La ruta pública base es `/api/products`. Las rutas y los campos de entrada y salida JSON usan nombres en inglés: `name`, `price`, `category` e `isActive`.

| Método | Ruta | Resultado |
| --- | --- | --- |
| GET | `/api/products` | `200 OK` con una lista de `ProductDto`, incluso si está vacía |
| GET | `/api/products/{id}` | `200 OK` con `ProductDto` o `404 Not Found` |
| POST | `/api/products` | `201 Created` con `ProductDto` y encabezado `Location`, o `400 Bad Request` |
| PUT | `/api/products/{id}` | `204 No Content`, `400 Bad Request` o `404 Not Found` |
| DELETE | `/api/products/{id}` | `204 No Content` o `404 Not Found` |

El body de creación y edición usa el mismo DTO:

```json
{
  "name": "Mechanical keyboard",
  "price": 45000.50,
  "category": "Peripherals"
}
```

`Name` es obligatorio y debe tener entre 3 y 100 caracteres. `Price` es obligatorio y debe ser estrictamente mayor que cero; usa `decimal` y admite fracciones positivas menores a un centavo. `Category` es obligatoria. Los campos de texto obligatorios no aceptan valores vacíos o compuestos solo por espacios.

La respuesta usa `ProductDto`:

```json
{
  "id": 1,
  "name": "Mechanical keyboard",
  "price": 45000.50,
  "category": "Peripherals",
  "isActive": true
}
```

Un producto se crea con `IsActive = true`. La edición conserva el ID y el estado `IsActive`, porque no forman parte de `CreateProductDto`. `DELETE` elimina físicamente el producto.

## Ejecutar y probar la API

### Desde Visual Studio 2026 Community

1. Abrir `SecondBlock.Products.API.slnx` desde la raíz del repositorio.
2. Esperar a que Visual Studio restaure los paquetes NuGet.
3. En el Explorador de soluciones, dentro de `src`, hacer clic derecho sobre `SecondBlock.Products.API` y elegir **Set as Startup Project / Establecer como proyecto de inicio**.
4. Seleccionar el perfil **http** o **https** en la barra de ejecución. El perfil `http` permite probar la aplicación sin configurar un certificado HTTPS.
5. Ejecutar con **F5** para depurar o **Ctrl+F5** para ejecutar sin depuración.
6. Se abrirá Swagger UI automáticamente en el navegador. Mantener la aplicación en ejecución durante las pruebas.

| Perfil | Swagger UI |
| --- | --- |
| `http` | [http://localhost:5184/swagger](http://localhost:5184/swagger) |
| `https` | [https://localhost:7181/swagger](https://localhost:7181/swagger) |

Si se usa `https` y falta confiar en el certificado de desarrollo, ejecutar `dotnet dev-certs https --trust` y volver a iniciar la aplicación.

### Desde la terminal

1. Desde la raíz, iniciar la API con el perfil HTTP:

   ```powershell
   dotnet run --project src/SecondBlock.Products.API/SecondBlock.Products.API.csproj --launch-profile http
   ```

2. Esperar el mensaje que indica que la aplicación está escuchando en `http://localhost:5184`.
3. Abrir [http://localhost:5184/swagger](http://localhost:5184/swagger) manualmente en el navegador. `dotnet run` no abre el navegador automáticamente.
4. Mantener la terminal abierta. Para detener la aplicación, pulsar **Ctrl+C**.

Para usar HTTPS desde la terminal, sustituir `--launch-profile http` por `--launch-profile https` y abrir `https://localhost:7181/swagger`.

### Probar el recorrido completo con Swagger UI

En Swagger UI, desplegar un endpoint de **Products**, seleccionar **Try it out**, completar el body o el parámetro `id` y pulsar **Execute**. La interfaz muestra el código HTTP, los encabezados y el body de la respuesta.

Ejemplo de body para `POST /api/products` y `PUT /api/products/{id}`:

```json
{
  "name": "Mechanical keyboard",
  "price": 45000.50,
  "category": "Peripherals"
}
```

Después de crear un producto, usar el `id` recibido para consultar, actualizar o eliminar ese producto.

| Paso | Operación | Resultado esperado |
| --- | --- | --- |
| 1 | Ejecutar `GET /api/products` después de iniciar la aplicación | `200 OK` con `[]` si todavía no se crearon productos |
| 2 | Ejecutar `POST /api/products` con el JSON de ejemplo | `201 Created`, un producto con `id` generado e `isActive: true` y un encabezado `Location` que apunta al GET del producto |
| 3 | Copiar el `id` recibido y ejecutar `GET /api/products/{id}` | `200 OK` con los datos del producto creado |
| 4 | Ejecutar `PUT /api/products/{id}` cambiando `name`, `price` o `category` | `204 No Content`, sin body |
| 5 | Volver a ejecutar `GET /api/products/{id}` | `200 OK` con los datos actualizados y el mismo `id` e `isActive` |
| 6 | Ejecutar `DELETE /api/products/{id}` | `204 No Content`, sin body |
| 7 | Consultar o eliminar otra vez ese mismo `id` | `404 Not Found` |

Para comprobar las validaciones, ejecutar un POST o PUT con este body:

```json
{
  "name": "AB",
  "price": 0,
  "category": ""
}
```

El resultado debe ser `400 Bad Request` con los errores de `Name`, `Price` y `Category`. Los datos inválidos no se guardan. Un PUT válido para un producto inexistente también debe devolver `404 Not Found`.

### Probar con el archivo HTTP de Visual Studio

1. Ejecutar la API con el perfil `http`.
2. Abrir `src/SecondBlock.Products.API/SecondBlock.Products.API.http`.
3. Usar **Send Request / Enviar solicitud** o el botón de ejecución de cada petición.
4. Ejecutar primero el POST válido y copiar el `id` de la respuesta.
5. Ajustar la variable `productId` del archivo y continuar con GET, PUT y DELETE.

La variable `host` usa `http://localhost:5184`. Si se elige otro puerto o perfil, actualizar esa variable.

Swagger UI y el documento OpenAPI están habilitados en `Development`, el entorno configurado en ambos perfiles. La interfaz usa `Swashbuckle.AspNetCore.SwaggerUI` y consume el documento existente en `/openapi/v1.json`, generado por `Microsoft.AspNetCore.OpenApi`, siguiendo la [integración documentada por ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/using-openapi-documents?view=aspnetcore-10.0).

## Compilar y ejecutar tests

Desde la raíz:

```powershell
dotnet build SecondBlock.Products.API.slnx
dotnet test SecondBlock.Products.API.slnx
```

La suite actual contiene **48 casos de prueba**:

| Proyecto | Casos | Alcance |
| --- | --- | --- |
| `SecondBlock.Products.API.UnitTests` | 9 | Respuestas del controlador y datos usados por `CreatedAtAction` |
| `SecondBlock.Products.Business.UnitTests` | 30 | Reglas de validación, mapeos y operaciones del servicio |
| `SecondBlock.Products.DataAccess.UnitTests` | 9 | CRUD del repositorio, listas de resultados, persistencia entre instancias e IDs |

Para ejecutarlos desde Visual Studio, abrir **Test > Test Explorer / Prueba > Explorador de pruebas**, compilar la solución y seleccionar **Run All / Ejecutar todas**. Las teorías de xUnit pueden mostrar varios casos bajo el mismo método de prueba.

Cada proyecto de tests referencia a su capa y tiene configurado el adaptador de xUnit para el Explorador de pruebas de Visual Studio. Las pruebas cubren respuestas del controlador, proyección de DTOs, reglas de validación y sus límites, creación, consulta, actualización y eliminación, persistencia entre instancias del repositorio, independencia de la lista de resultados y asignación secuencial de IDs. Los tests de controladores y servicios usan dobles de prueba para aislar sus dependencias; las validaciones automáticas del pipeline HTTP requieren ejecutar la API.

Para verificar también el contrato HTTP real después de compilar en Debug:

```powershell
dotnet build SecondBlock.Products.API.slnx --configuration Debug
./tests/Verify-Api.ps1
```

Este script para Windows inicia una instancia temporal de la API en un puerto libre, comprueba el recorrido CRUD, los errores `400` y `404`, el encabezado `Location`, la persistencia entre peticiones, OpenAPI y los archivos de Swagger UI, y detiene su instancia al terminar.

La salida esperada es `HTTP verification passed: 54 checks.`. El script usa su propio proceso y puerto, por lo que también se puede ejecutar mientras otra instancia de la API está abierta en Visual Studio.

Si aparece un aviso de puerto ocupado al iniciar manualmente la API, detener la instancia anterior o elegir otro puerto. Si Test Explorer conserva nombres de pruebas renombradas o eliminadas, limpiar y recompilar la solución, volver a abrirla y ejecutar **Run All**.

## Convención de commits

Usar el prefijo `feat:` seguido de una descripción breve en inglés. Ejemplo:

```text
feat: add three-layer products API with Swagger and tests
```

## Agregar proyectos

Al crear un proyecto desde Visual Studio, seleccionar `src/` o `tests/` como ubicación física y agregarlo a la carpeta de solución correspondiente.

Ejemplo desde la terminal para una nueva biblioteca y sus tests:

```powershell
dotnet new classlib -n ProjectName -o src/ProjectName -f net10.0
dotnet sln SecondBlock.Products.API.slnx add src/ProjectName/ProjectName.csproj --solution-folder src

dotnet new xunit -n ProjectName.UnitTests -o tests/ProjectName.UnitTests -f net10.0
dotnet add tests/ProjectName.UnitTests/ProjectName.UnitTests.csproj reference src/ProjectName/ProjectName.csproj
dotnet sln SecondBlock.Products.API.slnx add tests/ProjectName.UnitTests/ProjectName.UnitTests.csproj --solution-folder tests
```
