# DocumentationBuild

**GitHub Actions**

Can be used to automate the build-process of the HTML-documentation-files whenever a commit to the main branch is made. So the Docs can always stay up to date.

GitHub Action Documentation Build Config: [link]

**MkDocs**

A config file "/Docs/mkdocs.yml" needs to be created. Parameters for the build-process and output can be written in it. Mkdocs, when run, will basically then read the content-files from "/Docs/Content/" ('docs_dir') and give out the HTML-files into "/Docs/Site/" ('site_dir').