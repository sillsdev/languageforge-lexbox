# https://github.com/open-telemetry/opentelemetry-collector
FROM otel/opentelemetry-collector:0.73.0

COPY config.yaml /etc/config.yaml

# http receiver
EXPOSE 4318

CMD [ "--config=/etc/config.yaml" ]
