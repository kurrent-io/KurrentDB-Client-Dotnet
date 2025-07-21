import { dl } from "@mdit/plugin-dl";
import viteBundler from "@vuepress/bundler-vite";
import vueDevTools from 'vite-plugin-vue-devtools'
import {defineUserConfig} from "vuepress";
import {fs} from "vuepress/utils";
import {hopeTheme} from "vuepress-theme-hope";
import {linkCheckPlugin} from "./markdown/linkCheck";
import {replaceLinkPlugin} from "./markdown/replaceLink";
import {importCodePlugin} from "./markdown/xode/importCodePlugin";


export default defineUserConfig({
    base: "/",
    dest: "public",
    title: "KurrentDB Docs",
    description: "Event-native database",
    bundler: viteBundler({viteOptions: {plugins: [vueDevTools(),],}}),
    markdown: {importCode: false},
    extendsMarkdown: md => {
        md.use(linkCheckPlugin);
        md.use(replaceLinkPlugin, {
            replaceLink: (link: string, _) => link
                .replace("@api", "/api")
        });
        md.use(dl);
    },
    theme: hopeTheme({
        logo: "/eventstore-dev-logo-dark.svg",
        logoDark: "/eventstore-logo-alt.svg",
        docsDir: ".",
        toc: true,
        sidebar: {
            "/api/": "structure",
        },
        navbar: [
            {
                text: "API",
                link: "/api/getting-started/",
            },
        ],
        markdown: {
            figure: true,
            imgLazyload: true,
            imgMark: true,
            imgSize: true,
            tabs: true,
            codeTabs: true,
            component: true,
            mermaid: true,
            highlighter: {
                type: "shiki",
                themes: {
                    light: "one-light",
                    dark: "one-dark-pro",
                }
            }
        },
        plugins: {
            search: {},
            sitemap:{
                devServer: process.env.NODE_ENV === 'development',
                modifyTimeGetter: (page, app) =>
                    fs.statSync(app.dir.source(page.filePathRelative!)).mtime.toISOString()
            },
            components: {
                components: ["Badge", "VPBanner", "VPCard", "VidStack"]
            },
        }
    }),
});
