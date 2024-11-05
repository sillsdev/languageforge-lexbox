#!python
# version_settings() enforces a minimum Tilt version
# https://docs.tilt.dev/api.html#api.version_settings
version_settings(constraint='>=0.33.20')
secret_settings(disable_scrub=True)
config.define_bool("lexbox-api-local")
cfg = config.parse()
forward_lexbox = not cfg.get("lexbox-api-local", False)

docker_build(
    'local-dev-init',
    'data'
)

docker_build(
    'ghcr.io/sillsdev/lexbox-api',
    context='backend',
    dockerfile='./backend/LexBoxApi/dev.Dockerfile',
    only=['.'],
    ignore=['FwHeadless'],
    live_update=[
        sync('backend', '/src/backend')
    ]
)

docker_build(
    'ghcr.io/sillsdev/lexbox-fw-headless',
    context='backend',
    dockerfile='./backend/FwHeadless/dev.Dockerfile',
    only=['.'],
    ignore=['LexBoxApi'],
    live_update=[
        sync('backend', '/src/backend')
    ]
)

docker_build(
    'ghcr.io/sillsdev/lexbox-ui',
    context='frontend',
    dockerfile='./frontend/dev.Dockerfile',
    only=['.'],
    live_update=[
        sync('frontend', '/app'),
    ]
)

docker_build(
    'ghcr.io/sillsdev/lexbox-hgweb',
    context='hgweb',
    dockerfile='./hgweb/Dockerfile',
    only=['.'],
    build_args={"APP_VERSION": "dockerDev"},
    live_update=[
        sync('hgweb/repos', '/var/hg/repos'),
    ]
)

k8s_yaml(kustomize('./deployment/local-dev'))
allow_k8s_contexts('docker-desktop')

lexbox_ports = [
    port_forward(1080, name='maildev'),
    port_forward(18888, name='aspire'),
    port_forward(4318, name='otel') #otel
]
if forward_lexbox:
    lexbox_ports = [port_forward(5158, name='lexbox-api', link_path='/api/swagger')] + lexbox_ports

k8s_resource(
    'lexbox',
    labels=['app'],
    resource_deps=['db'],
    port_forwards=lexbox_ports
)
k8s_resource(
    'ui',
    labels=['app'],
    links=[link('http://localhost', 'ui')]
)
k8s_resource(
    'fw-headless',
    labels=['app'],
    resource_deps=['db'],
    port_forwards=[
        port_forward(5275, 80, name='fw-headless')
    ]
)
k8s_resource(
    'hg',
    labels=['app'],
    port_forwards=[
        port_forward(8088, name='hg'),
        port_forward(8034, 80, name='hg-resumable'),
    ]
)
k8s_resource(
    'db',
    port_forwards=[
        port_forward(5433, 5432, name='db'),
    ],
    labels=["db"]
)
k8s_resource(
    'pgadmin',
    resource_deps=['db'],
    port_forwards=[
        port_forward(4810, name='pgadmin'),
    ],
    labels=['db']
)
