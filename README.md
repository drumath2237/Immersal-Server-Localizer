# Immersal Server Localizer

[![Run EditMode Test](https://github.com/drumath2237/Immersal-Server-Localizer/actions/workflows/test.yml/badge.svg)](https://github.com/drumath2237/Immersal-Server-Localizer/actions/workflows/test.yml)

## About

ImmersalのREST APIを使用して空間の位置合わせを行うUnity Packageです。

## Tested Environments

- Unity 2020.3.11f
- UniTask 2.2.5
- ARFoundation 4.1.7
- Keijiro/Pcx

## Install

TBD

## Usage

`Assets/ImmersalRESTLocalizerTest/Settings/`以下に、
Createメニューから`Immersal REST Localizer`＞`ConfigurationScriptableObject`
でConfigファイルを作成し、Immersal Developer TokenとマップのID配列を
入力してください。

`Assets/ImmersalRESTLocalizerTest/Scenes/main.unity`
を開き、作成したconfigファイルをImmersalServerLocalizerコンポーネントに
アタッチします。

シーン中のAR Space以下に配置したオブジェクトが
位置合わせに適用されますので、お好みでImmersalのply点群などを配置してみてください。

このスクリプトではunsafeなコードが存在しますので、
Project SettingsからAllow Unsafeを有効にしてください。

## Contact

何かございましたら、[にー兄さんのTwitter](https://twitter.com/ninisan_drumath)までよろしくお願いいたします。
