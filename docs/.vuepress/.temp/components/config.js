import { hasGlobalComponent } from "C:/Code/KurrentDB-Client-Dotnet/docs/node_modules/.pnpm/@vuepress+helper@2.0.0-rc.74_typescript@5.5.3_vuepress@2.0.0-rc.19_@vuepress+bundler-vite@2.0_dtrpbp2lnh37xz2olmr2viykdm/node_modules/@vuepress/helper/lib/client/index.js";
import Badge from "C:/Code/KurrentDB-Client-Dotnet/docs/node_modules/.pnpm/vuepress-plugin-components@2.0.0-rc.71_sass-loader@15.0.0_sass@1.84.0__sass@1.84.0_typescript_soik3udsz4cusugodd4fz7leyq/node_modules/vuepress-plugin-components/lib/client/components/Badge.js";
import VPBanner from "C:/Code/KurrentDB-Client-Dotnet/docs/node_modules/.pnpm/vuepress-plugin-components@2.0.0-rc.71_sass-loader@15.0.0_sass@1.84.0__sass@1.84.0_typescript_soik3udsz4cusugodd4fz7leyq/node_modules/vuepress-plugin-components/lib/client/components/VPBanner.js";
import VPCard from "C:/Code/KurrentDB-Client-Dotnet/docs/node_modules/.pnpm/vuepress-plugin-components@2.0.0-rc.71_sass-loader@15.0.0_sass@1.84.0__sass@1.84.0_typescript_soik3udsz4cusugodd4fz7leyq/node_modules/vuepress-plugin-components/lib/client/components/VPCard.js";

import "C:/Code/KurrentDB-Client-Dotnet/docs/node_modules/.pnpm/@vuepress+helper@2.0.0-rc.74_typescript@5.5.3_vuepress@2.0.0-rc.19_@vuepress+bundler-vite@2.0_dtrpbp2lnh37xz2olmr2viykdm/node_modules/@vuepress/helper/lib/client/styles/sr-only.css";

export default {
  enhance: ({ app }) => {
    if(!hasGlobalComponent("Badge")) app.component("Badge", Badge);
    if(!hasGlobalComponent("VPBanner")) app.component("VPBanner", VPBanner);
    if(!hasGlobalComponent("VPCard")) app.component("VPCard", VPCard);
    
  },
  setup: () => {

  },
  rootComponents: [

  ],
};
