using System;
using System.Collections;
using System.Collections.Specialized;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

namespace DotNetOpenId.Provider {
	/// <summary>
	/// A request to verify the validity of a previous response.
	/// </summary>
	internal class CheckAuthRequest : AssociatedRequest {
		string signature;
		IDictionary<string, string> signedFields;
		IList<string> signedKeyOrder;
		string invalidate_handle;

		public CheckAuthRequest(OpenIdProvider server)
			: base(server) {
			AssociationHandle = Util.GetRequiredArg(Query, Protocol.Constants.openid.assoc_handle);
			signature = Util.GetRequiredArg(Query, Protocol.Constants.openid.sig);
			signedKeyOrder = Util.GetRequiredArg(Query, Protocol.Constants.openid.signed).Split(',');
			invalidate_handle = Util.GetOptionalArg(Query, Protocol.Constants.openid.invalidate_handle);

			signedFields = new Dictionary<string, string>();

			foreach (string key in signedKeyOrder) {
				string value = (key == Protocol.Constants.openidnp.mode) ?
					Protocol.Constants.Modes.id_res : Util.GetRequiredArg(Query, Protocol.Constants.openid.Prefix + key);
				signedFields.Add(key, value);
			}
		}

		public override bool IsResponseReady {
			// This type of request can always be responded to immediately.
			get { return true; }
		}

		/// <summary>
		/// Gets the string "check_authentication".
		/// </summary>
		internal override string Mode {
			get { return Protocol.Constants.Modes.check_authentication; }
		}

		/// <summary>
		/// Respond to this request.
		/// </summary>
		internal EncodableResponse Answer() {
			if (TraceUtil.Switch.TraceInfo) {
				Trace.TraceInformation("Start processing Response for CheckAuthRequest");
			}

			bool is_valid = Server.Signatory.Verify(AssociationHandle, signature, signedFields, signedKeyOrder);

			Server.Signatory.Invalidate(AssociationHandle, AssociationRelyingPartyType.Dumb);

			EncodableResponse response = new EncodableResponse(this);

			response.Fields[Protocol.Constants.openidnp.is_valid] = (is_valid ? "true" : "false");

			if (!string.IsNullOrEmpty(invalidate_handle)) {
				Association assoc = Server.Signatory.GetAssociation(invalidate_handle, AssociationRelyingPartyType.Smart);

				if (assoc == null) {
					if (TraceUtil.Switch.TraceWarning) {
						Trace.TraceWarning("No matching association found. Returning invalidate_handle. ");
					}

					response.Fields[Protocol.Constants.openidnp.invalidate_handle] = invalidate_handle;
				}
			}

			if (TraceUtil.Switch.TraceInfo) {
				Trace.TraceInformation("End processing Response for CheckAuthRequest. CheckAuthRequest response successfully created. ");
				if (TraceUtil.Switch.TraceVerbose) {
					Trace.TraceInformation("Response follows: {0}", response);
				}
			}

			return response;
		}

		internal override IEncodable CreateResponse() {
			return Answer();
		}

		public override string ToString() {
			string returnString = @"
CheckAuthRequest._sig = '{0}'
CheckAuthRequest.AssocHandle = '{1}'
CheckAuthRequest._invalidate_handle = '{2}' ";
			return base.ToString() + string.Format(CultureInfo.CurrentUICulture, 
				returnString, signature, AssociationHandle, invalidate_handle);
		}

	}
}
