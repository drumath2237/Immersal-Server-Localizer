# Immersal Server Localizer Sample

## About

ImmersalのREST APIを使用して空間の位置合わせを行うサンプル。
ARFoundationに対応しているモバイル端末で使用可能です。

## Environments

- Unity 2020.3.11f
- UniTask 2.2.5
- ARFoundation 4.1.7
- Keijiro/Pcx

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

## More info

~~ARFoundationでカメラ画像を取得する際に、途中からエラーになってしまう現象があります。原因不明なので解決できるかわからないです......。~~

(2021/9/11追記)

非同期メソッドを使っていてXRCpuImageのDisposeのタイミングをミスっていたらしいです。
現在は修正されています。

## Contact

何かございましたら、[にー兄さんのTwitter](https://twitter.com/ninisan_drumath)までよろしくお願いいたします。
