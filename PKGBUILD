# Maintainer: <haltroy> <thehaltroy@gmail.com>
pkgname=bluelabel
pkgver=1.0.0.0
pkgrel=1
pkgdesc="Organize files easier by labeling each file."
url="https://haltroy.com/BlueLabel"
license=(GPL-3.0-or-later)
provides=(bluelabel)
arch=(x86_64 aarch64)
depends=(vlc)
makedepends=(dotnet-sdk)
options=("!strip")

build() {
  if [ "$arch" = "x86_64" ]
  then
        dotnet publish \
        -c Release \
        -r linux-x64 \
        -o ../$pkgname \
        BlueLabel.Linux/BlueLabel.Linux.csproj
  elif [ "$arch" = "aarch64" ]
  then
        dotnet publish \
        -c Release \
        -r linux-arm64 \
        -o ../$pkgname \
        BlueLabel.Linux/BlueLabel.Linux.csproj
  fi
  rm ../$pkgname/BlueLabel.Linux.dbg
}

package() {
  mkdir -p "$pkgdir/opt/haltroy/"
  cp -r "../$pkgname" "$pkgdir/opt/haltroy/"
  install -d "$pkgdir/opt/haltroy"
}
