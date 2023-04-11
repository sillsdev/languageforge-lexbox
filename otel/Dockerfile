# https://github.com/open-telemetry/opentelemetry-collector
FROM otel/opentelemetry-collector:0.73.0

COPY collector-config.yaml /etc/collector-config.yaml

# http receiver
EXPOSE 4318

CMD [ "--config=/etc/collector-config.yaml" ]
