FROM dxflrs/garage:v2.2.0 AS garage

FROM alpine:3.21
RUN apk add --no-cache gettext
COPY --from=garage /garage /usr/local/bin/garage
COPY garage.toml.template /etc/garage/garage.toml.template
COPY entrypoint.sh /entrypoint.sh
RUN chmod +x /entrypoint.sh

VOLUME ["/var/lib/garage/meta", "/var/lib/garage/data"]

ENTRYPOINT ["/entrypoint.sh"]
