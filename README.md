# Subastón

Plataforma web de subastas en línea para comprar y vender **videojuegos, consolas y artículos coleccionables**. Los vendedores publican productos, los compradores pujan en tiempo real y la plataforma administra usuarios, pagos y comisiones.

## Proyecto desarrollado como parte de mi formación de Técnico en Informática en el CECyT No. 13 "Ricardo Flores Magón".

<img width="1203" height="717" alt="image" src="https://github.com/user-attachments/assets/40b9da8a-1878-4280-97b7-40a806a1b99e" />


## Funcionalidades

- **Sistema de pujas** y seguimiento de subastas.
- **Gestión de usuarios, productos y publicaciones.**
- **Roles con áreas separadas:** Admin, Comprador, Vendedor y Cuentas.
- **Pagos en línea** con Amazon Pay y MercadoPago.
- **Cálculo de comisiones:** la plataforma, el procesador de pagos e IVA se descuentan y el vendedor ve su ganancia neta por separado.
- **Localizador de sucursales** para vendedores.
- **Aviso de privacidad** aceptado durante el registro.
- **Generación de comprobantes en PDF** desde el navegador.

## Seguridad

- Middleware con rutas protegidas por rol.
- Cookies de sesión reforzadas.
- Encabezados `Cache-Control` para evitar volver a páginas privadas con el botón "atrás" después de cerrar sesión.
- Secretos y llaves fuera del repositorio (ver [Configuración](#️-configuración)).

## Arquitectura

Aplicación **ASP.NET Core MVC** organizada por **Areas**:

```
Areas/
├── Admin/       # Panel de administración
├── Comprador/   # Pujas, compras y seguimiento
├── Vendedor/    # Publicaciones, ventas y ganancias
└── Cuentas/     # Registro, inicio de sesión y perfil
Controllers/
Middleware/
Services/
Views/
```

## Tecnologías

| Programación por Capas | Tecnología |
| Backend | C#, ASP.NET Core MVC, Razor Pages |
| Frontend | HTML5, CSS, JavaScript |
| Base de datos | Supabase (PostgreSQL) |
| Pagos | Amazon Pay, MercadoPago |
| Nube | Microsoft Azure |

## Cómo ejecutarlo

### Requisitos

- [.NET SDK](https://dotnet.microsoft.com/download) (la versión del proyecto)
- Un proyecto en [Supabase](https://supabase.com)
- Visual Studio 2022 o el CLI de .NET

### Pasos

```bash
git clone https://github.com/el-commites/Subaston.git
cd Subaston
dotnet restore
dotnet run
```

###  Configuración

Las llaves **no** están en el repositorio. Configúralas con *user secrets* (los nombres deben coincidir con los de tu `appsettings.json`):

```bash
dotnet user-secrets init
dotnet user-secrets set "Supabase:Url" "TU_URL"
dotnet user-secrets set "Supabase:Key" "TU_LLAVE"
dotnet user-secrets set "MercadoPago:AccessToken" "TU_TOKEN"
```

##  Capturas

<img width="1426" height="672" alt="image" src="https://github.com/user-attachments/assets/3f6b66f2-281a-4c89-9a03-e30624264600" />

<img width="1353" height="725" alt="image" src="https://github.com/user-attachments/assets/f93afffa-0350-41d0-9116-20e5d9167ef0" />

<img width="1387" height="497" alt="image" src="https://github.com/user-attachments/assets/acdcd540-fc45-43be-8b81-b3faffe96873" />

<img width="1048" height="567" alt="image" src="https://github.com/user-attachments/assets/b9f57b63-3705-4942-915b-47bd685a7977" />

##  Autores
**Angel Santiago Camarena Gracia**
**Martin Paulo Mendoza Escobar**
**Ivan Emiliano Montes Garcia**
- GitHub: [@el-commites](https://github.com/el-commites)
- LinkedIn: [Tu enlace]
- Correo: [Tu correo]
