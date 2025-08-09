module.exports = {
  packagerConfig: {
    icon: 'assets/citisync.ico',
    extraResource: [
      'resources/backend'
    ],
    ignore: [
      /starcitisync\.db$/i
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