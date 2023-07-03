# https://wiki.mercurial-scm.org/modwsgi

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

# Configure Apache
COPY httpd.conf /usr/local/apache2/conf/httpd.conf
COPY hg.conf /usr/local/apache2/conf/sites/hg.conf

# Ensure /var/hg and /var/hg/repos are owned by www-data user
RUN install -d /var/hg/repos -o www-data -g www-data

# Configure hgweb
COPY hgweb.hgrc /var/hg/
COPY hgweb.wsgi /usr/local/www/wsgi-scripts/

VOLUME /var/hg/repos
