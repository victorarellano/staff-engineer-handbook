# Kubernetes --- Guía personal de estudio y referencia

> Guía construida a partir de mis apuntes del curso de Kubernetes.\
> Objetivo: entender **qué problema resuelve cada concepto**, recordar **cuándo usar cada comando** y disponer de una referencia rápida para laboratorios y troubleshooting.

------------------------------------------------------------------------

## Índice

1.  [Cómo usar esta guía](#1-cómo-usar-esta-guía)
2.  [Mapa mental de Kubernetes](#2-mapa-mental-de-kubernetes)
3.  [Entorno de laboratorio: WSL, Docker, kubectl y
    Minikube](#3-entorno-de-laboratorio-wsl-docker-kubectl-y-minikube)
4.  [Pods](#4-pods)
5.  [Manifiestos YAML y trabajo declarativo](#5-manifiestos-yaml-y-trabajo-declarativo)
6.  [Labels y Selectors](#6-labels-y-selectors)
7.  [Deployments y ReplicaSets](#7-deployments-y-replicasets)
8.  [Services y acceso a aplicaciones](#8-services-y-acceso-a-aplicaciones)
9.  [Aplicación distribuida: Redis + Frontend](#9-aplicación-distribuida-redis--frontend)
10. [Namespaces](#10-namespaces)
11. [Eventos y troubleshooting](#11-eventos-y-troubleshooting)
12. [Rolling Updates y Rollback](#12-rolling-updates-y-rollback)
13. [Variables de entorno](#13-variables-de-entorno)
14. [ConfigMaps](#14-configmaps)
15. [Secrets](#15-secrets)
16. [Kubeconfig, clusters, usuarios y contextos](#16-kubeconfig-clusters-usuarios-y-contextos)
17. [Scheduler, Node Selectors y Node Affinity](#17-scheduler-node-selectors-y-node-affinity)
18. [Laboratorio pendiente: cluster Kubernetes sobre Ubuntu](#18-laboratorio-pendiente-cluster-kubernetes-sobre-ubuntu)
19. [Cheat Sheet de kubectl](19. Cheat Sheet de kubectl)
20. [Ruta de troubleshooting](#20-ruta-de-troubleshooting)
21. [Temas pendientes para ampliar](#21-temas-pendientes-para-ampliar)

------------------------------------------------------------------------

# 1. Cómo usar esta guía

Esta guía no intenta ser una documentación completa de Kubernetes. Está organizada alrededor de los temas estudiados en el curso y de los laboratorios realizados.

Para cada concepto conviene recordar cuatro preguntas:

1.  **¿Qué problema resuelve?**
2.  **¿Qué objeto de Kubernetes interviene?**
3.  **¿Qué comandos uso para administrarlo?**
4.  **¿Qué comandos uso cuando algo falla?**

La idea no es memorizar cientos de comandos. Es recordar el modelo mental que permite deducir qué herramienta necesitamos.

------------------------------------------------------------------------

# 2. Mapa mental de Kubernetes

Una aplicación típica administrada por Kubernetes puede visualizarse así:

``` mermaid
flowchart TD
    U[Usuario / Cliente] --> S[Service]
    S --> P1[Pod]
    S --> P2[Pod]
    S --> P3[Pod]

    D[Deployment] --> RS[ReplicaSet]
    RS --> P1
    RS --> P2
    RS --> P3

    CM[ConfigMap] -. configuración .-> P1
    CM -. configuración .-> P2
    SEC[Secret] -. datos sensibles .-> P1
    SEC -. datos sensibles .-> P2
```

La relación importante para recordar es:

``` text
Deployment
    ↓ administra
ReplicaSet
    ↓ mantiene
Pods
    ↑ seleccionados mediante labels
Service
    ↓ ofrece un punto estable de acceso
Aplicación
```

### Idea clave

Un **Pod** ejecuta la aplicación, pero normalmente no queremos administrar Pods manualmente.

Un **Deployment** expresa el estado deseado de la aplicación y administra ReplicaSets, que mantienen las réplicas necesarias.

Un **Service** proporciona una forma estable de acceder a un conjunto de Pods.

Los **Labels** y **Selectors** permiten relacionar objetos.

------------------------------------------------------------------------

# 3. Entorno de laboratorio: WSL, Docker, kubectl y Minikube

## 3.1 Componentes del entorno

En los apuntes el laboratorio local se construyó principalmente con:

-   Windows + WSL/Ubuntu
-   Docker
-   `kubectl`
-   Minikube
-   Visual Studio Code

``` mermaid
flowchart LR
    WIN[Windows] --> WSL[WSL / Ubuntu]
    WSL --> KUBECTL[kubectl]
    WSL --> DOCKER[Docker]
    WSL --> MINI[Minikube]
    KUBECTL --> MINI
    MINI --> CLUSTER[Kubernetes local]
```

------------------------------------------------------------------------

## 3.2 Instalación de kubectl

`kubectl` es el cliente de línea de comandos utilizado para comunicarnos con la API de Kubernetes.

``` bash
curl -LO "https://dl.k8s.io/release/$(curl -L -s https://dl.k8s.io/release/stable.txt)/bin/linux/amd64/kubectl"

sudo install -o root -g root -m 0755 kubectl /usr/local/bin/kubectl
```

### ¿Qué resuelve?

Permite administrar Kubernetes desde una terminal:

``` bash
kubectl get pods
kubectl get nodes
kubectl apply -f deployment.yaml
kubectl logs mi-pod
```

### Verificación del binario descargado

``` bash
curl -LO "https://dl.k8s.io/release/$(curl -L -s https://dl.k8s.io/release/stable.txt)/bin/linux/amd64/kubectl.sha256"

echo "$(cat kubectl.sha256)  kubectl" | sha256sum --check
```

Esto comprueba que el archivo descargado coincide con el checksum publicado.

### Consultar versión

``` bash
kubectl version --output=yaml
```

------------------------------------------------------------------------

## 3.3 WSL y systemd

Consultar configuración:

``` bash
cat /etc/wsl.conf
```

Configuración anotada:

``` ini
[boot]
systemd=true
```

Comandos WSL utilizados:

``` bash
wsl
wsl -l
wsl -l -o
wsl --install -d Ubuntu
```

------------------------------------------------------------------------

## 3.4 Instalación de Minikube

Minikube permite crear un cluster Kubernetes local para desarrollo y aprendizaje.

``` bash
curl -LO https://github.com/kubernetes/minikube/releases/latest/download/minikube-linux-amd64

sudo install minikube-linux-amd64 /usr/local/bin/minikube && rm minikube-linux-amd64
```

En el laboratorio también se consideró habilitar virtualización en BIOS:

-   Intel VT-x
-   AMD-V

### Crear el cluster

``` bash
minikube start --driver=docker
```

### Comprobar estado

``` bash
minikube status
kubectl get nodes
kubectl get po -A
```

La secuencia mental es:

``` mermaid
flowchart LR
    START[minikube start] --> STATUS[minikube status]
    STATUS --> NODES[kubectl get nodes]
    NODES --> PODS[kubectl get pods -A]
```

------------------------------------------------------------------------

## 3.5 Administración de Minikube

``` bash
minikube stop
minikube delete
minikube logs
minikube ip
minikube ssh
minikube profile list
```

### Perfiles

Los perfiles permiten mantener clusters Minikube independientes.

``` bash
minikube start -p clusterDev --driver=docker --nodes=1
```

Cambiar de perfil:

``` bash
minikube profile clusterDev
```

Eliminar un perfil:

``` bash
minikube delete -p clusterDev
```

### Configuración

``` bash
minikube config set memory 4G -p minikube
minikube config get memory
```

### Dashboard

``` bash
minikube dashboard
```

### Directorios importantes

``` text
~/.minikube
~/.kube
```

`~/.minikube` contiene información relacionada con Minikube.

`~/.kube` contiene normalmente la configuración utilizada por `kubectl`, especialmente `config`.

------------------------------------------------------------------------

# 4. Pods

## 4.1 ¿Qué problema resuelve un Pod?

Kubernetes necesita una unidad mínima sobre la que ejecutar contenedores. Esa unidad es el **Pod**.

Un Pod puede contener uno o varios contenedores que comparten determinados recursos de red y almacenamiento.

``` mermaid
flowchart TB
    POD[Pod]
    POD --> C1[Contenedor nginx]
    POD --> NET[Red del Pod]
    POD --> VOL[Volúmenes]
```

En producción normalmente no se crean Pods individuales para aplicaciones que deben mantenerse disponibles. Para eso utilizaremos controladores como Deployments.

------------------------------------------------------------------------

## 4.2 Crear Pods rápidamente

``` bash
kubectl run nginx1 --image=nginx
```

Crea un Pod llamado `nginx1` usando la imagen `nginx`.

Otro ejemplo de los apuntes:

``` bash
kubectl run apache --image=httpd --port=8080
```

------------------------------------------------------------------------

## 4.3 Consultar Pods

``` bash
kubectl get pods
```

Primera pregunta:

> ¿Qué Pods existen y en qué estado están?

Para obtener más información:

``` bash
kubectl get pods -o wide
```

`-o wide` agrega datos adicionales, por ejemplo nodo e IP cuando están disponibles.

------------------------------------------------------------------------

## 4.4 Investigar un Pod

``` bash
kubectl describe pod/nginx1
```

Este es uno de los comandos fundamentales de troubleshooting.

Úsalo cuando:

-   un Pod no inicia;
-   permanece `Pending`;
-   tiene problemas al descargar una imagen;
-   quieres revisar configuración y estado;
-   necesitas consultar eventos asociados.

Modelo mental:

``` text
get       → ¿qué está pasando?
describe  → ¿por qué está pasando?
logs      → ¿qué dice la aplicación?
exec      → ¿qué ocurre dentro del contenedor?
```

------------------------------------------------------------------------

## 4.5 Ejecutar comandos dentro de un Pod

``` bash
kubectl exec nginx1 -- ls
kubectl exec nginx1 -- uname -a
```

Para abrir una terminal interactiva:

``` bash
kubectl exec -it nginx1 -- bash
```

> La shell disponible depende de la imagen. Algunas imágenes mínimas no incluyen `bash`.

------------------------------------------------------------------------

## 4.6 Logs

``` bash
kubectl logs apache
```

Seguir los logs continuamente:

``` bash
kubectl logs -f nginx1
```

`-f` significa *follow*.

En un Pod con varios contenedores hay que indicar cuál:

``` bash
kubectl logs multi -c frontal
kubectl logs -f multi -c frontal
kubectl exec multi -c frontal -- date
```

------------------------------------------------------------------------

## 4.7 Eliminar Pods

``` bash
kubectl delete pod/nginx1
```

Con período de gracia:

``` bash
kubectl delete pod nginx1 --grace-period=5
```

Eliminación inmediata:

``` bash
kubectl delete pod nginx1 --now
```

------------------------------------------------------------------------

## 4.8 Política de reinicio

Los valores correctos de `restartPolicy` son:

``` yaml
restartPolicy: Always
restartPolicy: OnFailure
restartPolicy: Never
```

> En las notas originales aparecía `OnFailer`; aquí se corrige a `OnFailure`.

------------------------------------------------------------------------

## 4.9 kubectl proxy

``` bash
kubectl proxy
```

Por defecto puede levantar un proxy local:

``` text
127.0.0.1:8001
```

Esto permite interactuar con la API de Kubernetes mediante el proxy local.

------------------------------------------------------------------------

## 4.10 Port forwarding

``` bash
kubectl port-forward pod/nginx1 9999:80
```

Interpretación:

``` text
localhost:9999
      │
      ▼
kubectl port-forward
      │
      ▼
Pod nginx1:80
```

Luego:

``` text
http://localhost:9999
```

### ¿Cuándo usarlo?

Principalmente para desarrollo, pruebas y debugging.

El proceso necesita permanecer activo mientras se utiliza el túnel.

------------------------------------------------------------------------

# 5. Manifiestos YAML y trabajo declarativo

Crear objetos manualmente es útil para aprender y para operaciones rápidas, pero Kubernetes está diseñado para trabajar declarativamente.

Ejemplo conceptual:

``` yaml
apiVersion: v1
kind: Pod
metadata:
  name: nginx
spec:
  containers:
    - name: nginx
      image: nginx
```

Aplicar:

``` bash
kubectl apply -f nginx.yaml
```

------------------------------------------------------------------------

## 5.1 create vs apply

En los apuntes aparecen ambos:

``` bash
kubectl create -f deploy_nginx.yaml
kubectl apply -f deploy_nginx.yaml
```

Una forma útil de recordarlo:

``` text
create → crear un recurso
apply  → declarar/aplicar el estado deseado
```

Para archivos mantenidos como configuración del proyecto, `apply` encaja naturalmente con un flujo declarativo.

------------------------------------------------------------------------

## 5.2 Generar YAML con kubectl

``` bash
kubectl create deployment nginx-test \
  --image=<REGISTRY>/lab-nginx-v1 \
  --dry-run=client \
  -o yaml > deployment.yaml
```

Esto resulta útil para generar una base YAML que posteriormente podemos editar.

------------------------------------------------------------------------

## 5.3 Flujo Docker → Registry → Kubernetes

Tus prácticas incluyen la construcción de una imagen antes de desplegarla.

``` mermaid
flowchart LR
    SRC[Código + Dockerfile] --> BUILD[docker build]
    BUILD --> IMG[Imagen]
    IMG --> REG[Registry]
    REG --> POD[Pod Kubernetes]
```

Comandos del flujo:

``` bash
docker build -t uirovac:lab-nginx-v1 .
docker tag uirovac:lab-nginx-v1 <REGISTRY>/lab-nginx-v1
docker images
docker login
docker push <REGISTRY>/lab-nginx-v1
docker pull <REGISTRY>/lab-nginx-v1
```

Probar localmente:

``` bash
docker run -d -p 80:80 --name nginx <REGISTRY>/lab-nginx-v1
docker stop nginx
```

> Las credenciales y tokens presentes en las notas originales fueron omitidos deliberadamente.

------------------------------------------------------------------------

# 6. Labels y Selectors

## 6.1 ¿Qué problema resuelven?

Kubernetes necesita una manera flexible de clasificar y encontrar objetos.

Los **labels** son pares clave/valor asociados a recursos.

Ejemplos:

``` text
app=nginx
version=v1
estado=desarrollo
responsable=juan
```

Los **selectors** permiten buscar objetos usando esos labels.

``` mermaid
flowchart LR
    S[Selector app=web] --> P1[Pod app=web]
    S --> P2[Pod app=web]
    X[Pod app=redis]
```

------------------------------------------------------------------------

## 6.2 Consultar labels

``` bash
kubectl get pods -o wide --show-labels
```

Mostrar una etiqueta como columna:

``` bash
kubectl get pods --show-labels -L app
```

------------------------------------------------------------------------

## 6.3 Añadir una etiqueta

``` bash
kubectl label pod tomcat responsable=Juan
```

Modificarla:

``` bash
kubectl label --overwrite pod tomcat responsable=Victor
```

Eliminarla:

``` bash
kubectl label pod tomcat responsable-
```

------------------------------------------------------------------------

## 6.4 Selectores

``` bash
kubectl get pods --show-labels -l estado=desarrollo
kubectl get pods --show-labels -l responsable=pedro
kubectl get pods --show-labels -l version=v1
```

Varias condiciones:

``` bash
kubectl get pods --show-labels -l 'estado=testing,responsable=pedro'
```

Conjuntos:

``` bash
kubectl get pods --show-labels -l 'estado in (desarrollo)'
kubectl get pods --show-labels -l 'estado notin (desarrollo,testing)'
```

Eliminar todos los Pods que coinciden:

``` bash
kubectl delete pods -l estado=desarrollo
```

------------------------------------------------------------------------

# 7. Deployments y ReplicaSets

## 7.1 ¿Qué problema resuelve un Deployment?

Si creamos solamente un Pod y este desaparece, necesitamos algún mecanismo que mantenga el estado deseado.

El Deployment permite declarar, entre otras cosas:

> Quiero que esta aplicación esté ejecutándose con N réplicas.

``` mermaid
flowchart TD
    D[Deployment nginx<br/>replicas: 3] --> RS[ReplicaSet]
    RS --> P1[Pod 1]
    RS --> P2[Pod 2]
    RS --> P3[Pod 3]
```

------------------------------------------------------------------------

## 7.2 Crear un Deployment

``` bash
kubectl create deployment apache --image=httpd
```

Consultar objetos relacionados:

``` bash
kubectl get deploy
kubectl get replicaset
kubectl get pods
```

Investigar:

``` bash
kubectl describe deploy apache
kubectl get deploy apache -o yaml
```

------------------------------------------------------------------------

## 7.3 Crear desde YAML

``` bash
kubectl apply -f deploy_nginx.yaml
```

Consultar Deployment, Pods y ReplicaSets relacionados mediante labels:

``` bash
kubectl get deploy,pods,rs -l app=nginx
```

------------------------------------------------------------------------

## 7.4 Editar

``` bash
kubectl edit deploy nginx-d
```

O modificar el YAML y volver a aplicar:

``` bash
kubectl apply -f deploy_nginx.yaml
```

El segundo enfoque permite conservar la configuración en Git.

------------------------------------------------------------------------

## 7.5 Escalar

``` bash
kubectl scale deploy nginx-d --replicas=5
```

La intención es:

``` text
estado actual: 2 Pods
estado deseado: 5 Pods
                  ↓
             Kubernetes
                  ↓
             crea 3 Pods
```

También se puede seleccionar Deployments por etiqueta:

``` bash
kubectl label --overwrite deploy nginx-d estado="1"
kubectl scale deploy -l estado=1 --replicas=2
```

------------------------------------------------------------------------

# 8. Services y acceso a aplicaciones

## 8.1 El problema

Los Pods son dinámicos. Pueden desaparecer y ser reemplazados.

Por eso no queremos que otros componentes dependan directamente de la IP particular de un Pod.

Un **Service** proporciona un punto de acceso estable a un conjunto de Pods.

``` mermaid
flowchart LR
    CLIENT[Cliente] --> SVC[Service]
    SVC --> P1[Pod]
    SVC --> P2[Pod]
    SVC --> P3[Pod]
```

------------------------------------------------------------------------

## 8.2 ClusterIP

Está pensado para acceso interno desde el cluster.

``` bash
kubectl expose deploy redis-master \
  --port=6379 \
  --type=ClusterIP
```

Un cliente dentro del cluster puede utilizar el nombre del Service:

``` bash
redis-cli -h redis-master
```

------------------------------------------------------------------------

## 8.3 NodePort

Expone el Service mediante un puerto del nodo.

``` bash
kubectl expose deploy apache2 \
  --port=80 \
  --type=NodePort \
  --name=apache2-svc
```

En Minikube:

``` bash
minikube service apache2-svc
```

------------------------------------------------------------------------

## 8.4 LoadBalancer

``` bash
kubectl expose deployment nginx-test \
  --type=LoadBalancer \
  --port=80
```

El tipo `LoadBalancer` está diseñado para integrarse con una implementación capaz de aprovisionar o proporcionar un balanceador externo.

En Minikube se puede utilizar:

``` bash
minikube tunnel
```

------------------------------------------------------------------------

## 8.5 Consultar Services

``` bash
kubectl get svc
kubectl describe svc apache2-svc
```

------------------------------------------------------------------------

## 8.6 Endpoints

En los laboratorios se consultaron endpoints con:

``` bash
kubectl get endpoints
kubectl get endpoints -o wide
kubectl get endpoints lab-web-svc -o yaml
```

Modelo mental:

``` mermaid
flowchart LR
    S[Service<br/>selector app=web] --> E[Endpoints]
    E --> P1[Pod IP 1]
    E --> P2[Pod IP 2]
    E --> P3[Pod IP 3]
```

Si un Service existe pero no llega a la aplicación, revisar la relación entre **selector del Service** y **labels de los Pods** es una comprobación importante.

------------------------------------------------------------------------

# 9. Aplicación distribuida: Redis + Frontend

Tus apuntes incluyen una práctica formada por:

-   Redis master
-   Redis slave
-   Services
-   Frontend
-   DNS interno

La arquitectura conceptual puede representarse así:

``` mermaid
flowchart LR
    USER[Usuario] --> FS[Frontend Service]
    FS --> F[Frontend Pods]
    F --> RMS[Redis Master Service]
    RMS --> RM[Redis Master]
    F --> RSS[Redis Slave Service]
    RSS --> RS1[Redis Slave]
```

------------------------------------------------------------------------

## 9.1 Redis master

``` bash
kubectl apply -f redis-master.yaml
kubectl get deployment,rs,pod -l app=redis
kubectl get all -l app=redis
kubectl apply -f redis-master-service.yaml
kubectl get svc -l app=redis
```

Entrar al Pod:

``` bash
kubectl exec -it <redis-master-pod> -- bash
```

Durante la práctica se instalaron herramientas de diagnóstico:

``` bash
apt-get update
apt-get install iputils-ping
apt-get install wget
apt-get install dnsutils
```

Consultar configuración DNS:

``` bash
cat /etc/resolv.conf
```

------------------------------------------------------------------------

## 9.2 Redis slave

``` bash
kubectl apply -f redis-slave.yaml
kubectl get all -l app=redis
kubectl apply -f redis-slave-service.yaml
kubectl get svc -l app=redis
```

------------------------------------------------------------------------

## 9.3 Frontend

``` bash
kubectl apply -f frontend.yaml
kubectl get all -l app=guestbook
kubectl apply -f frontend-service.yaml
kubectl get all -l app=guestbook
```

En Minikube:

``` bash
minikube service frontend
```

### Qué conviene recordar del laboratorio

El valor del ejercicio no está únicamente en los YAML. Permite entender que una aplicación Kubernetes normalmente está formada por **varios objetos que colaboran**:

``` text
Deployment → mantiene Pods
Service    → permite encontrarlos
DNS        → permite usar nombres
Labels     → relacionan recursos
```

------------------------------------------------------------------------

# 10. Namespaces

## 10.1 ¿Qué problema resuelven?

Los Namespaces permiten organizar y separar lógicamente recursos dentro de un cluster.

``` mermaid
flowchart TB
    C[Cluster]
    C --> DEV[namespace: dev]
    C --> QA[namespace: qa]
    C --> PROD[namespace: prod]
```

------------------------------------------------------------------------

## 10.2 Consultar

``` bash
kubectl get namespace
kubectl describe namespace default
kubectl get pods -n kube-system
kubectl get pods -n default
```

------------------------------------------------------------------------

## 10.3 Crear

``` bash
kubectl create namespace n1
```

Desde manifiesto:

``` bash
kubectl apply -f namespace.yaml
```

------------------------------------------------------------------------

## 10.4 Trabajar dentro de un Namespace

``` bash
kubectl apply -f deploy_elastic.yaml -n dev1
kubectl get deploy elastic -n dev1
kubectl describe deploy elastic -n dev1
kubectl get pods -n dev1
```

------------------------------------------------------------------------

## 10.5 Namespace predeterminado del contexto

Consultar configuración:

``` bash
kubectl config view
```

Establecer:

``` bash
kubectl config set-context --current --namespace=dev1
```

Después, los comandos que no indiquen `-n` utilizarán ese namespace en el contexto actual.

------------------------------------------------------------------------

## 10.6 Eliminar

``` bash
kubectl delete namespace n1
```

> Eliminar un Namespace implica eliminar los recursos namespaced que contiene. Revisar siempre qué hay dentro antes de hacerlo.

------------------------------------------------------------------------

# 11. Eventos y troubleshooting

Los eventos ayudan a explicar decisiones y errores observados por Kubernetes.

``` bash
kubectl get events -n desarrollo
```

Solo warnings:

``` bash
kubectl get events -n desarrollo \
  --field-selector type=Warning
```

Filtrar por razón:

``` bash
kubectl get events -n desarrollo \
  --field-selector reason=FailedScheduling
```

Observar continuamente:

``` bash
kubectl get events -n desarrollo -w
```

------------------------------------------------------------------------

## 11.1 Secuencia de diagnóstico

Cuando algo falla:

``` mermaid
flowchart TD
    A[La aplicación no funciona] --> B[kubectl get pods -o wide]
    B --> C{Pod Running?}
    C -- No --> D[kubectl describe pod]
    D --> E[kubectl get events]
    C -- Sí --> F[kubectl logs]
    F --> G{Necesito inspeccionar dentro?}
    G -- Sí --> H[kubectl exec]
    G -- No --> I[Revisar Service / selectors / endpoints]
```

### Regla práctica

``` text
1. get
2. describe
3. events
4. logs
5. exec
6. revisar Service / labels / endpoints
```

------------------------------------------------------------------------

# 12. Rolling Updates y Rollback

Los Deployments permiten actualizar una aplicación manteniendo un historial de revisiones.

Consultar historial:

``` bash
kubectl rollout history deploy nginx-d
```

Consultar una revisión:

``` bash
kubectl rollout history deploy nginx-d --revision=1
```

Consultar progreso:

``` bash
kubectl rollout status deploy nginx-d
```

Ver Pods y ReplicaSets:

``` bash
kubectl get pods
kubectl get rs
```

Deshacer:

``` bash
kubectl rollout undo deploy nginx-d
```

Modelo mental:

``` mermaid
flowchart LR
    V1[Versión v1] --> UPDATE[Rolling Update]
    UPDATE --> V2[Versión v2]
    V2 -->|problema| UNDO[rollout undo]
    UNDO --> V1B[Revisión anterior]
```

------------------------------------------------------------------------

# 13. Variables de entorno

Una aplicación puede recibir configuración mediante variables de entorno.

Después de desplegar:

``` bash
kubectl apply -f var1.yaml
kubectl get pods
```

Entrar:

``` bash
kubectl exec -it var-ejemplo -- bash
```

Consultar:

``` bash
printenv
```

Esto prepara el camino para entender ConfigMaps y Secrets.

------------------------------------------------------------------------

# 14. ConfigMaps

## 14.1 ¿Qué problema resuelve un ConfigMap?

Permite separar configuración no sensible de la imagen de la aplicación.

Sin ConfigMap:

``` text
Imagen de aplicación
 └── configuración incluida dentro
```

Con ConfigMap:

``` mermaid
flowchart LR
    CM[ConfigMap] --> POD[Pod]
    IMG[Imagen] --> POD
```

La misma imagen puede ejecutarse con configuraciones diferentes.

------------------------------------------------------------------------

## 14.2 Crear desde literales

``` bash
kubectl create configmap cf1 \
  --from-literal=usuario=usu1 \
  --from-literal=password=pass1
```

> Aunque este ejemplo procede del laboratorio, las contraseñas reales deberían tratarse como datos sensibles y no como ConfigMaps.

Consultar:

``` bash
kubectl get configmap
kubectl get cm
kubectl describe cm cf1
kubectl get cm cf1 -o yaml
```

------------------------------------------------------------------------

## 14.3 Crear desde archivo

``` bash
kubectl create configmap datos-mysql \
  --from-file=datos_mysql.properties
```

Consultar:

``` bash
kubectl describe cm datos-mysql
kubectl get cm datos-mysql -o yaml
```

------------------------------------------------------------------------

## 14.4 Crear desde archivo de variables

``` bash
kubectl create configmap datos-mysql-env \
  --from-env-file=datos_mysql.properties
```

Después un Pod puede consumir esos valores como variables de entorno.

Comprobar:

``` bash
kubectl exec -it pod11 -- bash
printenv
```

------------------------------------------------------------------------

## 14.5 ConfigMap como volumen

Otro patrón estudiado es montar configuración como archivos dentro del contenedor.

``` mermaid
flowchart LR
    CM[ConfigMap] --> VOL[Volumen]
    VOL --> PATH[Archivos dentro del Pod]
```

Consultar:

``` bash
kubectl get cm config-volumen -o yaml
kubectl exec -it pod1 -- bash
```

------------------------------------------------------------------------

# 15. Secrets

## 15.1 ¿Qué problema resuelven?

Secrets proporcionan un objeto específico para almacenar y entregar información sensible a los workloads.

Ejemplos:

-   contraseñas;
-   tokens;
-   credenciales de registry;
-   certificados y claves.

### Corrección importante de los apuntes

**Base64 no es cifrado.**

En manifiestos y salidas de Kubernetes ciertos datos de Secret pueden aparecer codificados en Base64. Codificar no equivale a cifrar.

``` text
texto → Base64 → texto codificado
                  ↓
            reversible fácilmente
```

Por lo tanto:

> No asumir que un Secret está protegido simplemente porque el valor aparece en Base64.

------------------------------------------------------------------------

## 15.2 Secret genérico

``` bash
kubectl create secret generic passwords \
  --from-literal=pass-root=<PASSWORD_ROOT> \
  --from-literal=pass-usu=<PASSWORD_USER>
```

Consultar:

``` bash
kubectl get secret
kubectl get secret passwords -o yaml
```

------------------------------------------------------------------------

## 15.3 Secret desde archivo

``` bash
kubectl create secret generic datos \
  --from-file=datos.txt
```

------------------------------------------------------------------------

## 15.4 Base64

Codificar:

``` bash
echo -n "usu1" | base64
```

Ejemplo para comprender el mecanismo:

``` bash
echo -n "<PASSWORD>" | base64
```

------------------------------------------------------------------------

## 15.5 Secret como volumen

``` mermaid
flowchart LR
    S[Secret] --> V[Volume]
    V --> P[/tmp/datos dentro del Pod]
```

Después de desplegar:

``` bash
kubectl exec -it pod1 -- bash
cd /tmp/datos
ls -l
```

------------------------------------------------------------------------

## 15.6 Secret para un registry Docker

El laboratorio utilizó un Secret de tipo Docker Registry.

``` bash
kubectl create secret docker-registry midocker \
  --docker-server=https://index.docker.io/v1/ \
  --docker-username=<DOCKER_USERNAME> \
  --docker-password=<DOCKER_TOKEN> \
  --docker-email=<EMAIL>
```

Consultar:

``` bash
kubectl get secrets
kubectl get secret midocker -o yaml
```

Este tipo de Secret puede utilizarse para permitir que Kubernetes descargue imágenes privadas.

> Nunca guardar tokens reales en este repositorio Git.

------------------------------------------------------------------------

## 15.7 Otros tipos anotados en el curso

Los apuntes mencionan:

-   Service Account Token
-   Basic Auth
-   SSH
-   TLS
-   Bootstrap

Estos temas quedaron solamente anotados y no hay suficiente material en las notas originales para desarrollarlos como laboratorios completos.

------------------------------------------------------------------------

# 16. Kubeconfig, clusters, usuarios y contextos

## 16.1 Modelo mental

`kubectl` necesita saber:

1.  a qué cluster conectarse;
2.  qué credenciales utilizar;
3.  qué contexto está activo.

``` mermaid
flowchart TD
    K[kubeconfig]
    K --> C[Clusters]
    K --> U[Users / Credentials]
    K --> CTX[Contexts]
    CTX --> C
    CTX --> U
    CTX --> NS[Namespace]
    CURRENT[current-context] --> CTX
```

------------------------------------------------------------------------

## 16.2 Comandos principales

``` bash
kubectl config set-cluster
kubectl config set-credentials
kubectl config set-context
kubectl config use-context
kubectl config view
```

------------------------------------------------------------------------

## 16.3 Consultar configuración

``` bash
kubectl config view
kubectl config view -o json
kubectl config view -o yaml
kubectl config current-context
```

Cambiar contexto:

``` bash
kubectl config use-context minikube
```

------------------------------------------------------------------------

## 16.4 Configurar un cluster

Ejemplo estudiado:

``` bash
kubectl config set-cluster cluster2 \
  --server=https://1.2.3.4
```

Con autoridad certificadora:

``` bash
kubectl config set-cluster cluster2 \
  --certificate-authority=~/dev/cert.crt
```

Utilizando un kubeconfig alternativo:

``` bash
kubectl --kubeconfig=config_alternativo \
  config set-cluster cluster3 \
  --server=https://1.2.3.4
```

------------------------------------------------------------------------

## 16.5 Credenciales

Con certificado:

``` bash
kubectl config set-credentials usu2 \
  --client-certificate=fichero.crt \
  --client-key=fichero.key
```

Los apuntes también contienen un ejemplo de usuario/password. Conviene tratar cualquier credencial real como secreta y no almacenarla en Git.

------------------------------------------------------------------------

## 16.6 Contextos

``` bash
kubectl config set-context contexto-cluster2 \
  --user=usu1 \
  --namespace=desarrollo
```

Activar:

``` bash
kubectl config use-context contexto-cluster2
```

Un contexto puede recordarse como:

``` text
contexto = cluster + identidad + namespace
```

------------------------------------------------------------------------

# 17. Scheduler, Node Selectors y Node Affinity

## 17.1 ¿Qué problema resuelve el Scheduler?

Cuando se crea un Pod, Kubernetes necesita decidir **en qué nodo ejecutarlo**.

``` mermaid
flowchart TD
    P[Pod pendiente] --> S[Scheduler]
    S --> N1[Worker 1]
    S --> N2[Worker 2]
    S --> N3[Worker 3]
```

------------------------------------------------------------------------

## 17.2 Consultar dónde quedó un Pod

``` bash
kubectl get pods -o wide
```

La salida permite relacionar cada Pod con su nodo.

------------------------------------------------------------------------

## 17.3 Labels en nodos

Consultar:

``` bash
kubectl get nodes --show-labels
```

Etiquetar:

``` bash
kubectl label node k8s-workerlab1 entorno=desarrollo
```

Filtrar:

``` bash
kubectl get nodes --show-labels -l entorno=desarrollo
```

------------------------------------------------------------------------

## 17.4 Node Selector

Un Pod puede pedir ejecutarse solamente en nodos que tengan determinada etiqueta.

Ejemplo conceptual:

``` yaml
spec:
  nodeSelector:
    entorno: desarrollo
```

Flujo:

``` mermaid
flowchart LR
    P[Pod<br/>nodeSelector:<br/>entorno=desarrollo]
    P --> S[Scheduler]
    S --> N1[Node A<br/>entorno=desarrollo]
    S -. no coincide .-> N2[Node B<br/>entorno=produccion]
```

En el laboratorio:

``` bash
kubectl label node k8s-workerlab1 entorno=desarrollo
kubectl get pods -o wide
```

------------------------------------------------------------------------

## 17.5 Etiquetado para una aplicación

``` bash
kubectl label node k8s-workerlab1 aplicacion=web
kubectl get nodes --show-labels -l aplicacion=web
kubectl apply -f deploy-nginx.yaml
kubectl get pods -o wide
```

------------------------------------------------------------------------

## 17.6 Node Affinity

Los apuntes avanzan posteriormente hacia afinidad:

``` bash
kubectl apply -f pod_affinity.yaml
kubectl get nodes
kubectl label node k8s-workerlab2 equipo=desarrollo-web
kubectl get nodes --show-labels -l equipo=desarrollo-web
kubectl get pods -o wide
```

La idea general estudiada es controlar o influir en qué nodos pueden ejecutarse determinados Pods mediante características de los nodos.

``` mermaid
flowchart TD
    P[Pod] --> A[Reglas de Node Affinity]
    A --> N1[Node compatible]
    A -. descarta/no prefiere .-> N2[Node no compatible]
```

------------------------------------------------------------------------

# 18. Laboratorio: cluster Kubernetes sobre Ubuntu

El laboratorio de construcción de un cluster Kubernetes multi-nodo con Ubuntu, VMware, kubeadm, containerd y Flannel se mantiene como un documento independiente para no duplicar contenido en esta guía.

➡️ **[Ver laboratorio: Kubernetes con kubeadm sobre VMware](laboratorio-kubernetes-kubeadm-vmware.md)**

El laboratorio documenta la preparación de una VM base, clonación del control plane y workers, configuración de containerd, instalación de kubeadm/kubelet/kubectl, inicialización del cluster, instalación de Flannel, incorporación de workers y comprobaciones posteriores.


---
# 19. Cheat Sheet de kubectl

## Consultar recursos

``` bash
kubectl get pods
kubectl get pods -o wide
kubectl get pods -A
kubectl get deploy
kubectl get rs
kubectl get svc
kubectl get nodes
kubectl get all
```

## Investigar

``` bash
kubectl describe pod <pod>
kubectl describe deploy <deployment>
kubectl describe svc <service>
kubectl get events
```

## Logs

``` bash
kubectl logs <pod>
kubectl logs -f <pod>
kubectl logs <pod> -c <container>
```

## Ejecutar comandos

``` bash
kubectl exec <pod> -- <comando>
kubectl exec -it <pod> -- bash
```

## Aplicar YAML

``` bash
kubectl apply -f archivo.yaml
kubectl apply -f .
```

## Eliminar

``` bash
kubectl delete -f archivo.yaml
kubectl delete pod <pod>
kubectl delete deploy <deployment>
kubectl delete svc <service>
```

## Escalar

``` bash
kubectl scale deploy <deployment> --replicas=3
```

## Rollout

``` bash
kubectl rollout status deploy <deployment>
kubectl rollout history deploy <deployment>
kubectl rollout undo deploy <deployment>
```

## Labels

``` bash
kubectl get pods --show-labels
kubectl label pod <pod> clave=valor
kubectl label --overwrite pod <pod> clave=nuevo-valor
kubectl label pod <pod> clave-
kubectl get pods -l clave=valor
```

## Namespaces

``` bash
kubectl get ns
kubectl create namespace <namespace>
kubectl get pods -n <namespace>
kubectl config set-context --current --namespace=<namespace>
```

## Contextos

``` bash
kubectl config view
kubectl config current-context
kubectl config use-context <context>
```

## Port-forward

``` bash
kubectl port-forward pod/<pod> 9999:80
```

------------------------------------------------------------------------

# 20. Ruta de troubleshooting

Esta sección está pensada para consultarla cuando algo no funciona.

## Caso A --- El Pod no inicia

``` bash
kubectl get pods -o wide
kubectl describe pod <pod>
kubectl get events
```

Después revisar:

-   imagen;
-   estado del contenedor;
-   eventos;
-   nodo asignado;
-   configuración;
-   Secrets/ConfigMaps requeridos.

------------------------------------------------------------------------

## Caso B --- El Pod inicia pero la aplicación falla

``` bash
kubectl logs <pod>
kubectl logs -f <pod>
```

Si hay varios contenedores:

``` bash
kubectl logs <pod> -c <container>
```

Si hace falta inspeccionar:

``` bash
kubectl exec -it <pod> -- bash
```

------------------------------------------------------------------------

## Caso C --- El Service no responde

Consultar:

``` bash
kubectl get svc
kubectl describe svc <service>
kubectl get endpoints
kubectl get pods --show-labels
```

Pensar en esta cadena:

``` mermaid
flowchart LR
    CLIENT[Cliente] --> SERVICE[Service]
    SERVICE --> SELECTOR[Selector]
    SELECTOR --> LABEL[Labels]
    LABEL --> POD[Pod]
    POD --> APP[Aplicación escuchando]
```

Si se rompe cualquiera de esas relaciones, el tráfico no llegará como esperamos.

------------------------------------------------------------------------

## Caso D --- El Pod está Pending

``` bash
kubectl describe pod <pod>
kubectl get events
kubectl get nodes
kubectl get nodes --show-labels
```

Si estamos trabajando con selectors/affinity, comprobar que exista algún nodo que satisfaga las reglas.

------------------------------------------------------------------------

## Caso E --- Una actualización salió mal

``` bash
kubectl rollout status deploy <deployment>
kubectl rollout history deploy <deployment>
kubectl get rs
kubectl get pods
```

Si corresponde volver a la revisión anterior:

``` bash
kubectl rollout undo deploy <deployment>
```

------------------------------------------------------------------------

# 21. Temas pendientes para ampliar

Esta guía refleja el contenido disponible en los apuntes actuales. Los siguientes temas pueden incorporarse conforme avance el estudio:

-   Ingress
-   PersistentVolumes y PersistentVolumeClaims
-   StorageClasses
-   StatefulSets
-   DaemonSets
-   Jobs y CronJobs
-   Liveness, Readiness y Startup Probes
-   Requests y Limits
-   RBAC
-   ServiceAccounts en mayor profundidad
-   NetworkPolicies
-   Helm
-   Horizontal Pod Autoscaler
-   Taints y Tolerations
-   Pod Affinity / Anti-Affinity
-   Kubernetes DNS en mayor profundidad
-   instalación multi-node sobre Ubuntu
-   observabilidad y métricas

------------------------------------------------------------------------

# Resumen mental final

Cuando necesite recordar Kubernetes:

``` mermaid
flowchart TD
    A[Quiero ejecutar una aplicación] --> P[Pod]
    P --> D[Quiero que Kubernetes la mantenga<br/>Deployment]
    D --> R[Quiero varias copias<br/>Replicas]
    R --> S[Quiero acceder de forma estable<br/>Service]
    S --> L[¿Cómo encuentra los Pods?<br/>Labels + Selectors]
    L --> C[Configuración no sensible<br/>ConfigMap]
    C --> SE[Información sensible<br/>Secret]
    SE --> N[Separar recursos<br/>Namespace]
    N --> SC[Decidir dónde ejecutar<br/>Scheduler]
    SC --> T[Algo falla<br/>get → describe → events → logs → exec]
```

La idea más importante no es memorizar comandos aislados:

> **Kubernetes mantiene un estado deseado mediante objetos declarativos y controladores.**

Los comandos son las herramientas que utilizamos para **declarar, observar, modificar y diagnosticar** ese estado.

------------------------------------------------------------------------

## Seguridad de estas notas

Las notas originales contenían credenciales y tokens utilizados durante laboratorios. En esta versión se han reemplazado por placeholders como:

``` text
<DOCKER_USERNAME>
<DOCKER_TOKEN>
<PASSWORD>
<EMAIL>
```

No almacenar secretos reales en un repositorio Git.

------------------------------------------------------------------------

**Documento en evolución.**\
El próximo capítulo práctico pendiente es recuperar e incorporar el laboratorio de instalación de un cluster Kubernetes sobre Ubuntu.
