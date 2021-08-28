﻿// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using LibreLancer.Shaders;
using LibreLancer.Vertices;
using LibreLancer.Utf.Mat;
namespace LibreLancer
{
	public class DetailMapMaterial : RenderMaterial
	{
		public string DmSampler;
		public SamplerFlags DmFlags;
		public float TileRate;
		public int FlipU;
		public int FlipV;
		public Color4 Ac;
		public Color4 Dc;
		public string DtSampler;
		public SamplerFlags DtFlags;


		public override void Use (RenderState rstate, IVertexType vertextype, ref Lighting lights)
		{
			rstate.DepthEnabled = true;
			rstate.BlendMode = BlendMode.Opaque;

            var sh = Shaders.DetailMapMaterial.Get(GL.GLES ? ShaderFeatures.VERTEX_LIGHTING : 0);
			sh.SetWorld (World);
            sh.SetView(Camera);
            sh.SetViewProjection(Camera);

			sh.SetAc(Ac);
			sh.SetDc(Dc);
			sh.SetTileRate(TileRate);
			sh.SetFlipU(FlipU);
			sh.SetFlipV(FlipV);

			sh.SetDtSampler(0);
			BindTexture (rstate, 0, DtSampler, 0, DtFlags);
			sh.SetDmSampler(1);
			BindTexture (rstate, 1, DmSampler, 1, DmFlags);
			SetLights(sh, ref lights, Camera.FrameNumber);
            sh.UseProgram ();
		}

		public override void ApplyDepthPrepass(RenderState rstate)
		{
			rstate.BlendMode = BlendMode.Normal;
            var sh = Shaders.DepthPass_Normal.Get();
            sh.SetWorld(World);
            sh.SetViewProjection(Camera);
            sh.UseProgram();
		}

		public override bool IsTransparent
		{
			get
			{
				return false;
			}
		}
	}
}

