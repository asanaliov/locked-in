#!/bin/sh
# Installs Locked In for the current user, without root. Run it again to update.
set -eu

source_dir=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
data_home=${XDG_DATA_HOME:-$HOME/.local/share}
app_dir=$HOME/.local/opt/locked-in
bin_dir=$HOME/.local/bin

if [ "${1:-}" = "--uninstall" ]; then
    rm -rf "$app_dir"
    rm -f "$bin_dir/locked-in" "$data_home/applications/locked-in.desktop" \
        "$data_home/icons/hicolor/512x512/apps/locked-in.png" \
        "${XDG_CONFIG_HOME:-$HOME/.config}/autostart/locked-in.desktop"
    echo "Locked In is uninstalled. Your data is kept in $data_home/LockedIn; delete that folder to remove it too."
    exit 0
fi

rm -rf "$app_dir"
mkdir -p "$app_dir" "$bin_dir" "$data_home/applications" "$data_home/icons/hicolor/512x512/apps"
cp -R "$source_dir/app/." "$app_dir/"
cp "$source_dir/install.sh" "$app_dir/uninstall.sh"
ln -sf "$app_dir/locked-in" "$bin_dir/locked-in"
cp "$source_dir/locked-in.png" "$data_home/icons/hicolor/512x512/apps/locked-in.png"
sed "s|^Exec=.*|Exec=\"$app_dir/locked-in\"|" "$source_dir/locked-in.desktop" > "$data_home/applications/locked-in.desktop"

echo "Locked In is installed. Open it from your app menu, or run: $bin_dir/locked-in"
echo "To uninstall: sh $app_dir/uninstall.sh --uninstall"
