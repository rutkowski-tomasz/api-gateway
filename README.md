# 🔀 Api.Gateway

Lightweight performant API Gateway 

Built with .NET 10 and YARP, designed to route and manage traffic to multiple microservices within docker/kubernetes network.

## 🚀 Features

- Reverse proxy functionality
- Multiple service, path-based routing
- Response compression (Brotli, gzip)
- CORS configuration
- Security headers (`nosniff`, `X-Frame-Options`, `Referrer-Policy`, optional CSP)
- Rate limiting
- Response caching (in memory, driven by downstream `Cache-Control` headers)
- Prometheus metrics at `/metrics`: request count and duration per service and status code (block `/metrics` at your ingress)
- Docker support
- CI integration with performance tests


## 👨🏻‍💻 Development

```sh
dotnet test # Run tests
docker-compose -f compose.yml up --build # Run locally
```

## 💡 Ideas

- Implement Authentication and Authorization (JWT integration with OpenID Connect)
- Bring your own certificate

## 🚫 Out of scope

- Automatic Let's Encrypt certificates: LettuceEncrypt is archived; use an ingress (cert-manager, Caddy) for automatic issuance
- SQL injection protection: pattern filtering at the gateway is easy to bypass and blocks valid input; use parameterized queries in the services or a dedicated WAF (OWASP CRS)
