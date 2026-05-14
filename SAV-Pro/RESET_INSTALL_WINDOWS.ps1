Write-Host "Cleaning installation..."
Remove-Item -Recurse -Force node_modules -ErrorAction SilentlyContinue
Remove-Item -Force package-lock.json -ErrorAction SilentlyContinue
npm config set registry https://registry.npmjs.org/
npm install --registry=https://registry.npmjs.org/
npm run dev
