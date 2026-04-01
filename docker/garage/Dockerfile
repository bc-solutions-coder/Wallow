FROM dxflrs/garage:v2.2.0 AS garage
FROM alpine:3.21
COPY --from=garage /garage /usr/local/bin/garage
COPY garage.toml /etc/garage/garage.toml
COPY init-garage.sh /etc/garage/init-garage.sh
RUN chmod +x /etc/garage/init-garage.sh
ENV GARAGE_CONFIG_FILE=/etc/garage/garage.toml
ENTRYPOINT ["/bin/sh", "/etc/garage/init-garage.sh"]
