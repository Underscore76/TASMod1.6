local view = {}

---swaps between different view states (base, map)
function view.next()
    interface:NextView()
end

---sets the view to the map view
function view.map()
    interface:SetView(TASView.Map)
end

---sets the view to the base view
function view.base()
    interface:SetView(TASView.Base)
end

---resets the view to the default state
function view.reset()
    interface:ResetView()
end

---sets the view to the location
function view.location(loc)
    interface:ViewLocation(loc)
end

return view
