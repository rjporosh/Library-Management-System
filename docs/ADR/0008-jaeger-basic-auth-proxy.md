# ADR 0008 - Jaeger UI behind an nginx basic-auth proxy

**Status:** accepted (2026-09-19)

**Context.** The Jaeger all-in-one image has no authentication; publishing its UI
on 16686 exposes every trace (including request paths and user ids) to anyone who
can reach the host. The request was to give Jaeger a real credential.

**Decision.** Jaeger's UI port is `expose`d only on the compose network. A small
`nginx:alpine` container (`jaeger-auth`) publishes 16686 and requires HTTP basic
auth against `docker/jaeger/.htpasswd` (default `jaeger` / `Jaeger@123`, apr1 hash).
OTLP ingest (4317) stays open because the API talks to it inside the network.

**Trade-offs.** Basic auth over plain HTTP is only appropriate for local/dev; behind
TLS or an ingress it is fine for staging. The default password is committed
deliberately (documented, dev-only) - regenerate the file for any real deployment
(command in `docker/jaeger/nginx.conf`). Alternatives rejected: OAuth proxy (heavy
for an MVP), Jaeger native auth (does not exist).
