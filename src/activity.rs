use chrono::{Utc, Duration};
use chrono::format::strftime::StrftimeItems;
use reqwest::Client;
use crate::models::{EventsResponse, Slot};


pub async fn get_activities(client: &Client) -> Result<Vec<Slot>, reqwest::Error>{
    let now = Utc::now();
    let eight_days_later = now + Duration::days(8);

    let format = StrftimeItems::new("%Y-%m-%dT%H:%M:%S%.3fZ");

    let from_offset = now.format_with_items(format.clone()).to_string();
    let to_time = eight_days_later.format_with_items(format.clone()).to_string();
    let url = format!(
        "https://www.aarhuskanokajak.dk/api/activity/event/days?eventsToShow=200&fromOffset={}&toTime={}",
        from_offset,
        to_time
    );

    let result = client.get(url).send().await?.json::<EventsResponse>().await?;
    
    return Ok(result.into_iter().flat_map(|event| event.slots.into_iter()).collect());
}

pub async fn sign_up_for_activity(client: &Client, slot: &Slot) -> Result<reqwest::Response, reqwest::Error>{
    let post_body = "{\"isValidationRequest\":false,\"memberAttendeeCount\":1,\"bookingText\":\"\",\"attendees\":[{\"memberId\":\"Member251786037605550929311\",\"attendeeCount\":1,\"doCancel\":false,\"doUpdate\":false,\"bookingText\":\"\"}],\"bookingActionOnPayment\":\"bookForFree\",\"sendMails\":true}".to_string();
    let url = format!("https://www.aarhuskanokajak.dk/api/activity/{}/event/{}/book", slot.activity_id, slot.event_id);
    return client.post(url).header("Content-Type", "application/json; charset=utf-8").body(post_body).send().await;
}