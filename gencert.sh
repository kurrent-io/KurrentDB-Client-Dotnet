#!/usr/bin/env bash

unameOutput="$(uname -sr)"
case "${unameOutput}" in
    Linux*Microsoft*) machine=WSL;;
    Linux*)           machine=Linux;;
    Darwin*)          machine=MacOS;;
    *)                machine="${unameOutput}"
esac

echo ">> Generating certificate..."
mkdir -p certs

chmod 0755 ./certs

docker pull docker.eventstore.com/eventstore-utils/es-gencert-cli:latest

docker run --rm --volume $PWD/certs:/tmp --user $(id -u):$(id -g) docker.eventstore.com/eventstore-utils/es-gencert-cli create-ca -out /tmp/ca -force

docker run --rm --volume $PWD/certs:/tmp --user $(id -u):$(id -g) docker.eventstore.com/eventstore-utils/es-gencert-cli create-node -ca-certificate /tmp/ca/ca.crt -ca-key /tmp/ca/ca.key -out /tmp/node -ip-addresses 127.0.0.1 -dns-names localhost -force

docker run --rm --volume $PWD/certs:/tmp --user $(id -u):$(id -g) docker.eventstore.com/eventstore-utils/es-gencert-cli create-user -username admin -ca-certificate /tmp/ca/ca.crt -ca-key /tmp/ca/ca.key -out /tmp/user-admin -force

docker run --rm --volume $PWD/certs:/tmp --user $(id -u):$(id -g) docker.eventstore.com/eventstore-utils/es-gencert-cli create-user -username invalid -ca-certificate /tmp/ca/ca.crt -ca-key /tmp/ca/ca.key -out /tmp/user-invalid -force

chmod -R 0755 ./certs

if [ "${machine}" == "MacOS" ]; then
#  echo ">> Installing certificate on ${machine}..."
#  sudo security add-trusted-cert -d -r trustRoot -k /Library/Keychains/System.keychain certs/ca/ca.crt
 echo ">> Installing certificate on ${machine}..."

   # Check certificate format and display info
   echo ">> Certificate details:"
   openssl x509 -in certs/ca/ca.crt -text -noout | head

   # Extract certificate Common Name
   CERT_NAME=$(openssl x509 -noout -subject -in certs/ca/ca.crt | sed -n 's/.*CN *= *\([^,]*\).*/\1/p')
   echo ">> Certificate Common Name: $CERT_NAME"

   # Copy certificate to a temporary location with proper permissions
   sudo cp certs/ca/ca.crt /tmp/ca.crt
   sudo chmod 644 /tmp/ca.crt

   # Install certificate
   echo ">> Importing certificate to System keychain..."
   sudo security import /tmp/ca.crt -k /Library/Keychains/System.keychain -T /usr/bin/codesign

   # Set trust settings
   echo ">> Setting trust settings..."
   sudo security add-trusted-cert -d -r trustRoot -p ssl -p basic -k /Library/Keychains/System.keychain /tmp/ca.crt

   # Clean up
   sudo rm /tmp/ca.crt

   # Verify installation
   echo ">> Verifying certificate installation..."
   if security find-certificate -a -c "$CERT_NAME" -p /Library/Keychains/System.keychain > /dev/null 2>&1; then
     echo ">> Certificate '$CERT_NAME' installed successfully in System keychain."
   else
     echo ">> Certificate installation failed. Please check Keychain Access manually."
     echo ">> You can try installing it manually by opening certs/ca/ca.crt in Keychain Access."
   fi
elif [ "$machine" == "Linux" ] || [ "$machine" == "WSL" ]; then
  echo ">> Copying certificate..."
  cp certs/ca/ca.crt /usr/local/share/ca-certificates/eventstore_ca.crt
  echo ">> Installing certificate on ${machine}..."
  sudo update-ca-certificates
else
  echo ">> Unknown platform. Please install the certificate manually."
fi
