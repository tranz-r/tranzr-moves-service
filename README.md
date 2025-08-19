# tranzr-moves-services


🔹 What ExtraHelperHours does

Right now, your code says:

decimal extraLabour = Math.Max(0, (decimal)(req.ExtraHelperHours - tier.IncludedHelperHours))
* config.ExtraHelperPerHour;


req.ExtraHelperHours = how many hours of extra labour this job needs (customer input or system-estimated).

tier.IncludedHelperHours = free labour hours that come built into the tier (priority/premium “come with more labour”).

If ExtraHelperHours > IncludedHelperHours, you charge for the difference × ExtraHelperPerHour.

If it’s less, the tier covers it for free.