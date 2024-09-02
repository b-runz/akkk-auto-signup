use serde::{Deserialize, Serialize};
#[derive(Serialize, Debug)]
pub struct LoginInformation {
    pub username : String,
    pub password : String,
    pub remember : bool
}

#[derive(Debug, Deserialize)]
pub struct Slot {
    #[serde(rename = "activityId")]
    pub activity_id: String,
    pub name: String,
    #[serde(rename = "eventId")]
    pub event_id: String,
}

#[derive(Debug, Deserialize)]
pub struct Event {
    pub slots: Vec<Slot>
}

pub type EventsResponse = Vec<Event>;