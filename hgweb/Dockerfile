# https://wiki.mercurial-scm.org/modwsgi

FROM httpd:2.4-alpine

ENV CUSTOM_PORT=8088

#install both mod_wsgi and mercurial
#we need to install mercurial via pip otherwise we get a really old version
RUN set -ex && \
    apk add apache2-mod-wsgi \
    py3-pip python3-dev build-base && \
    pip3 install --no-binary :all: mercurial && \
    apk del py3-pip python3-dev build-base

# Configure Apache
COPY httpd.conf /usr/local/apache2/conf/httpd.conf
COPY hg.conf /usr/local/apache2/conf/sites/hg.conf

# Configure hgweb
COPY hgweb.hgrc /var/hg/
COPY hgweb.wsgi /usr/local/www/wsgi-scripts/

VOLUME /var/hg/repos
