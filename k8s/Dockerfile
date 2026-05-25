FROM quay.io/keycloak/keycloak:latest

ENV KC_HEALTH_ENABLED=true
ENV KC_METRICS_ENABLED=true
ENV KC_DB=postgres

RUN /opt/keycloak/bin/kc.sh build