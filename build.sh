VERSION=v_$(git rev-parse --short=5 HEAD)
echo "build version $VERSION"
cd HttpClientTests
docker build -t httptest.azurecr.io/httpclienttests:$VERSION .
cd ..
az acr login --name httptest.azurecr.io
docker push httptest.azurecr.io/httpclienttests:$VERSION
cat deploy-disabled.yaml| envsubst | kubectl apply -f -
cat deploy-enabled.yaml| envsubst | kubectl apply -f -
cat deploy-test.yaml| envsubst | kubectl apply -f -