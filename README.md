# 🔀 Api.Gateway

Lightweight performant API Gateway 

Built with .NET 9 and YARP, designed to route and manage traffic to multiple microservices within docker/kubernetes network.

## 🚀 Features

- Reverse proxy functionality
- Multiple service, path-based routing
- Response compression (Brotli, gzip)
- CORS configuration
- Security headers (`nosniff`, `X-Frame-Options`, `Referrer-Policy`, optional CSP)
- Rate limiting
- Response caching (in memory, driven by downstream `Cache-Control` headers)
- Docker support
- CI integration with performance tests


## 👨🏻‍💻 Development

```sh
dotnet test # Run tests
docker-compose -f compose.yml up --build # Run locally
```

## 💡 Ideas

- Implement Authentication and Authorization (JWT integration with OpenID Connect)
- Statistics requests with HTTP codes and duration (requires db)
- Certificate generation in Lets Encrypt (LettuceEncrypt)
- Bring your own certificate
- SQL injection protection