// Copyright (c) 2017 Leacme (http://leac.me). View LICENSE.md for more information.
using Godot;
using System;

public class Hud : Node2D {

	private AudioEffectRecord recordEffect;
	private AudioStreamSample rec;
	private Button recordBt = new Button();
	private Button playBt = new Button();
	private const string FILENAME = "recoryrec1.wav";

	private TextureRect vignette = new TextureRect() {
		Expand = true,
		Texture = new GradientTexture() {
			Gradient = new Gradient() { Colors = new[] { Colors.Transparent } }
		},
		Material = new ShaderMaterial() {
			Shader = new Shader() {
				Code = @"
					shader_type canvas_item;
					void fragment() {
						float iRad = 0.3;
						float oRad = 1.0;
						float opac = 0.5;
						vec2 uv = SCREEN_UV;
					    vec2 cent = uv - vec2(0.5);
					    vec4 tex = textureLod(SCREEN_TEXTURE, SCREEN_UV, 0.0);
					    vec4 col = vec4(1.0);
					    col.rgb *= 1.0 - smoothstep(iRad, oRad, length(cent));
					    col *= tex;
					    col = mix(tex, col, opac);
					    COLOR = col;
					}"
			}
		}
	};

	public override void _EnterTree() {
		var masterIndex = AudioServer.GetBusIndex("Master");
		AudioServer.AddBus(masterIndex + 1);
		AudioServer.SetBusName(masterIndex + 1, "Record");
		AudioServer.AddBusEffect(masterIndex + 1, new AudioEffectRecord(), 0);
		AudioServer.SetBusMute(masterIndex + 1, true);
	}

	public override void _Ready() {
		InitVignette();

		recordEffect = (AudioEffectRecord)AudioServer.GetBusEffect(AudioServer.GetBusIndex("Record"), 0);
		var recordNode = new AudioStreamPlayer() { Autoplay = true, Stream = new AudioStreamMicrophone(), Bus = "Record" };
		AddChild(recordNode);

		var btHolder = new VBoxContainer() { Alignment = BoxContainer.AlignMode.Center };
		btHolder.AddConstantOverride("separation", 60);
		btHolder.RectMinSize = GetViewportRect().Size;
		AddChild(btHolder);

		recordBt.Text = "Record";
		playBt.Text = "Play";

		StyleButton(recordBt, btHolder);
		StyleButton(playBt, btHolder);

		btHolder.AddChild(recordBt);
		btHolder.AddChild(playBt);

		recordBt.Connect("button_down", this, nameof(OnRecordDown));
		recordBt.Connect("button_up", this, nameof(OnRecordUp));
		playBt.Connect("button_down", this, nameof(OnPlayDown));
		playBt.Connect("button_up", this, nameof(OnPlayUp));

		if (!new File().FileExists("user://" + FILENAME)) {
			playBt.Disabled = true;
		}

	}

	private void OnRecordDown() {
		playBt.Disabled = true;
		if (!recordEffect.IsRecordingActive()) {
			recordEffect.SetRecordingActive(true);
		}

	}

	private void OnRecordUp() {
		playBt.Disabled = false;
		if (recordEffect.IsRecordingActive()) {
			recordEffect.SetRecordingActive(false);
			rec = recordEffect.GetRecording();
			rec.SaveToWav(System.IO.Path.Combine(OS.GetUserDataDir(), FILENAME));
		}
	}

	private void OnPlayDown() {
		recordBt.Disabled = true;
		var audioPlayer = GetTree().Root.GetNode<Main>("Main").Audio;
		if (!audioPlayer.Playing) {
			AudioStreamSample savedAudStream = new AudioStreamSample();
			using (var savedAudFile = new File()) {
				if (savedAudFile.FileExists("user://" + FILENAME)) {
					savedAudFile.Open("user://" + FILENAME, File.ModeFlags.Read);
					savedAudStream.Data = savedAudFile.GetBuffer((int)savedAudFile.GetLen());
					savedAudStream.Format = AudioStreamSample.FormatEnum.Format16Bits;
					savedAudStream.Stereo = true;
					savedAudFile.Close();
				}
			}
			savedAudStream.Play(audioPlayer);
		}
	}

	private void OnPlayUp() {
		recordBt.Disabled = false;
		GetTree().Root.GetNode<Main>("Main").Audio.Stop();
	}

	private void StyleButton(Button button, VBoxContainer btHolder) {
		button.SizeFlagsHorizontal = (int)Control.SizeFlags.ShrinkCenter;
		button.RectMinSize = new Vector2(btHolder.RectMinSize.x * 0.7f, 40);
		button.AddFontOverride("font", new DynamicFont() { FontData = GD.Load<DynamicFontData>("res://assets/default/Tuffy_Bold.ttf"), Size = 40 });
	}

	public override void _Draw() {
		DrawBorder(this);
	}

	private void InitVignette() {
		vignette.RectMinSize = GetViewportRect().Size;
		AddChild(vignette);
		if (Lib.Node.VignetteEnabled) {
			vignette.Show();
		} else {
			vignette.Hide();
		}
	}

	public static void DrawBorder(CanvasItem canvas) {
		if (Lib.Node.BoderEnabled) {
			var vps = canvas.GetViewportRect().Size;
			int thickness = 4;
			var color = new Color(Lib.Node.BorderColorHtmlCode);
			canvas.DrawLine(new Vector2(0, 1), new Vector2(vps.x, 1), color, thickness);
			canvas.DrawLine(new Vector2(1, 0), new Vector2(1, vps.y), color, thickness);
			canvas.DrawLine(new Vector2(vps.x - 1, vps.y), new Vector2(vps.x - 1, 1), color, thickness);
			canvas.DrawLine(new Vector2(vps.x, vps.y - 1), new Vector2(1, vps.y - 1), color, thickness);
		}
	}
}
