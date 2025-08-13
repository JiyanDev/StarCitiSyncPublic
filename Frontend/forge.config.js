module.exports = {
  packagerConfig: {
    icon: 'assets/citisync.ico',
    asar: true,
    extraResource: [
      'resources/backend'
    ],
    ignore: [
      /starcitisync\.db$/i,
      /\.git/,
      /README\.md$/,
      /test/,
      /\.gitignore$/,
      /\/node_modules\/\.cache/
    ]
  },
  makers: [
    {
      name: '@electron-forge/maker-squirrel',
      config: {
        name: 'starCitiSync',
        authors: 'JiyanDev',
        description: 'MissionTracker',
        setupIcon: 'assets/citisync.ico',
        shortcutName: 'StarCitiSync',
        createDesktopShortcut: true,
        createStartMenuShortcut: true,
      }
    }
  ],
  publishers: [
    {
      name: '@electron-forge/publisher-github',
      config: {
        repository: {
          owner: 'JiyanDev',
          name: 'StarCitiSyncPublic'
        },
        prerelease: false,
        draft: true
      }
    }
  ]
};