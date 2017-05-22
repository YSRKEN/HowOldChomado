﻿using Prism.Commands;
using Prism.Services;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel;

using HowOldChomado.Services;
using HowOldChomado.BusinessObjects;
using HowOldChomado.Repositories;

namespace HowOldChomado.ViewModels
{
    public class RegisterPageViewModel : BindableBase
    {
        private static readonly PropertyChangedEventArgs IsValidInputPropertyChangedEventArgs = new PropertyChangedEventArgs(propertyName: nameof(IsValidInput));
        private ICameraService CameraService { get; }
        private IPlayerRepository PlayerRepository { get; }
        private IFaceService FaceService { get; }
        private IPageDialogService PageDialogService { get; }

        public DelegateCommand TakePhotoCommand { get; }
        public DelegateCommand RegisterCommand { get; }

        private byte[] image;

        public byte[] Image
        {
            get { return this.image; }
            set { this.SetProperty(storage: ref this.image, value: value); }
        }

        private string name;
        public string Name
        {
            get { return this.name; }
            set
            {
                this.SetProperty(storage: ref this.name, value: value);
                this.OnPropertyChanged(IsValidInputPropertyChangedEventArgs);
            }
        }

        private string age;
        public string Age
        {
            get { return this.age; }
            set
            {
                this.SetProperty(storage: ref this.age, value: value);
                this.OnPropertyChanged(IsValidInputPropertyChangedEventArgs);
            }
        }

        public bool IsValidInput
        {
            get
            {
                if (string.IsNullOrWhiteSpace(this.Name))
                {
                    return false;
                }
                return int.TryParse(s: this.Age, result: out var _); // C# 7
            }
        }

        private bool isBusy;

        public bool IsBusy
        {
            get { return this.isBusy; }
            set { this.SetProperty(ref this.isBusy, value); }
        }


        public RegisterPageViewModel(
            ICameraService cameraService,
            IPlayerRepository playerRepository,
            IFaceService faceService,
            IPageDialogService pageDialogService
        )
        {
            this.CameraService = cameraService;
            this.PlayerRepository = playerRepository;
            this.FaceService = faceService;
            this.PageDialogService = pageDialogService;
            this.TakePhotoCommand = new DelegateCommand(async () => await this.TakePhotoAsync());
            this.RegisterCommand = new DelegateCommand(async () => await this.RegisterAsync())
                .ObservesCanExecute(canExecuteExpression: () => this.IsValidInput);
        }

        private async Task RegisterAsync()
        {
            this.IsBusy = true;
            try
            {
                if (this.Image == null)
                {
                    await this.PageDialogService.DisplayAlertAsync(title: "情報", message: "写真を追加してください", cancelButton: "OK");
                    return;
                }

                var player = await this.PlayerRepository.FindByDisplayNameAsync(displayName: this.Name);
                if (player != null)
                {
                    player = new Player
                    {
                        Age = int.Parse(this.Age),
                        DisplayName = this.Name,
                        PersonId = Guid.NewGuid().ToString(),
                        Picture = this.Image,
                    };
                    await this.PlayerRepository.AddAsync(player);
                }
                else
                {
                    var personId = await this.FaceService.CreateFaceAsync(new ImageRequest { Image = this.Image });
                    var newPlayer = new Player
                    {
                        Age = int.Parse(this.Age),
                        DisplayName = this.Name,
                        PersonId = personId.ToString(),
                        Picture = this.Image,
                    };
                    await this.PlayerRepository.AddAsync(newPlayer);
                }


                await this.PageDialogService.DisplayAlertAsync("情報", $"{this.Name}さんを登録しました", "OK");
                this.Name = "";
                this.Age = "";
                this.Image = null;
                await this.FaceService.RegisterFaceAsync(player.PersonId, new ImageRequest { Image = this.Image });
            }
            finally
            {
                this.IsBusy = false;
            }
        }

        private async Task TakePhotoAsync()
        {
            var result = await this.CameraService.TakePhotosAsync();
            if (result == null)
            {
                return;
            }
            this.Image = result;
        }
    }
}
