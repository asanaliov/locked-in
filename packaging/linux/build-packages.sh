#!/bin/sh
# Publishes the Linux app and packs it as artifacts/locked-in-VERSION-linux-x64.tar.gz and a .deb.
# Usage: packaging/linux/build-packages.sh 1.2.3
set -eu

version=${1:?usage: build-packages.sh VERSION}
root=$(CDPATH= cd -- "$(dirname -- "$0")/../.." && pwd)
packaging=$root/packaging/linux
artifacts=$root/artifacts
publish=$artifacts/linux/publish

rm -rf "$artifacts/linux"
dotnet publish "$root/src/LockedIn.Linux" -c Release -r linux-x64 --self-contained \
    -o "$publish" -p:Version="$version" -p:DebugType=none

# Tarball: a folder with an install script.
name=locked-in-$version-linux-x64
staging=$artifacts/linux/$name
mkdir -p "$staging"
cp -R "$publish" "$staging/app"
cp "$packaging/install.sh" "$packaging/locked-in.desktop" "$staging/"
cp "$root/docs/logo/logo.png" "$staging/locked-in.png"
chmod +x "$staging/install.sh" "$staging/app/locked-in"
tar -czf "$artifacts/$name.tar.gz" -C "$artifacts/linux" "$name"

# Debian package.
deb=$artifacts/linux/deb
mkdir -p "$deb/DEBIAN" "$deb/opt/locked-in" "$deb/usr/bin" "$deb/usr/share/applications" "$deb/usr/share/icons/hicolor/512x512/apps"
cp -R "$publish/." "$deb/opt/locked-in/"
chmod +x "$deb/opt/locked-in/locked-in"
ln -s /opt/locked-in/locked-in "$deb/usr/bin/locked-in"
cp "$packaging/locked-in.desktop" "$deb/usr/share/applications/"
cp "$root/docs/logo/logo.png" "$deb/usr/share/icons/hicolor/512x512/apps/locked-in.png"
cat > "$deb/DEBIAN/control" <<EOF
Package: locked-in
Version: $version
Section: utils
Priority: optional
Architecture: amd64
Maintainer: Asan <asanaliov@users.noreply.github.com>
Homepage: https://github.com/asanaliov/locked-in
Installed-Size: $(du -sk "$deb/opt" | cut -f1)
Depends: libc6, libgcc-s1, libstdc++6, zlib1g, libssl3 | libssl1.1, libx11-6, libxss1, libxext6, libxi6, libxcursor1, libxrandr2, libice6, libsm6, libfontconfig1
Description: Tracks your screen time and focus
 Records which app you're using, sorts each one into Focus, Neutral or
 Distraction, and gives you a daily Locked In Score. Everything stays on
 your machine.
EOF
dpkg-deb --root-owner-group --build "$deb" "$artifacts/locked-in_${version}_amd64.deb"

ls -la "$artifacts"/*.tar.gz "$artifacts"/*.deb
