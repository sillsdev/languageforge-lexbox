FROM debian:bookworm as otel-builder

# download the opentelemetry webserver sdk
WORKDIR /work
ADD https://github.com/open-telemetry/opentelemetry-cpp-contrib/releases/download/webserver%2Fv1.0.3/opentelemetry-webserver-sdk-x64-linux.tgz otel.tgz
RUN tar -xvf otel.tgz
RUN apt-get update  \
    && apt-get install -y --no-install-recommends python3-pip
# wsgi OTEL https://opentelemetry-python-contrib.readthedocs.io/en/latest/instrumentation/wsgi/wsgi.html
RUN pip3 install --target /work/wsgi-otel opentelemetry-instrumentation-wsgi opentelemetry-api opentelemetry-sdk opentelemetry-exporter-otlp-proto-grpc


# https://wiki.mercurial-scm.org/modwsgi
# https://hg.mozilla.org/mozilla-central/help/hgweb

FROM httpd:2.4-bookworm

ENV CUSTOM_PORT=8088

#install both mod_wsgi and mercurial
#we need to install mercurial via pip otherwise we get a really old version
RUN set -ex && \
    apt-get update && \
    apt-get install -y libapache2-mod-wsgi-py3 \
    mercurial && \
    apt-get autoremove --purge -y && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists

COPY --from=otel-builder /work/wsgi-otel /usr/lib/python3/dist-packages/
COPY --from=otel-builder /work/opentelemetry-webserver-sdk /opt/opentelemetry-webserver-sdk
RUN bash -c /opt/opentelemetry-webserver-sdk/install.sh

# Configure Apache
COPY httpd.conf /usr/local/apache2/conf/httpd.conf
COPY hg.conf /usr/local/apache2/conf/sites/hg.conf
COPY opentelemetry_module.conf /usr/local/apache2/conf/

# Ensure /var/hg and /var/hg/repos are owned by www-data user
RUN install -d /var/hg/repos -o www-data -g www-data

# Configure hgweb
COPY hgweb.hgrc /var/hg/
COPY hgweb.wsgi /usr/local/www/wsgi-scripts/
COPY --chmod=755 command-runner.sh /usr/local/www/commands/
COPY json-lex /usr/lib/python3/dist-packages/mercurial/templates/json-lex

VOLUME /var/hg/repos

ARG APP_VERSION
ENV APP_VERSION=$APP_VERSION
ENV OTEL_RESOURCE_ATTRIBUTES_FROM_BUILD="service.name=hgweb,service.version=${APP_VERSION}"
