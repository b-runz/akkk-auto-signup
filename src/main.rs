use dotenvy::{dotenv, from_filename};
use reqwest::cookie::Jar;
use reqwest::Client;
use std::sync::Arc;
use tokio;
pub mod models;
use crate::models::LoginInformation;
pub mod activity;
use crate::activity::{get_activities, sign_up_for_activity};
use std::env;

const LOGIN_URL: &str = "https://www.aarhuskanokajak.dk/account/loginajax";

#[tokio::main]
pub async fn main() -> Result<(), reqwest::Error> {
    let cookie_store = Arc::new(Jar::default());
    let client = Client::builder()
        .cookie_store(true)
        .cookie_provider(cookie_store.clone())
        .build()?;

    dotenv().ok();
    
    let login_info = LoginInformation {
            username: env::var("USERNAME").expect("username not set"),
            password: env::var("PASSWORD").expect("password not set"),
            remember: false,
        };

    let loginreponse = client.post(LOGIN_URL).json(&login_info).send().await?;

    if loginreponse.status().is_success() {
        println!("Login successful!");
    } else {
        eprintln!("Login failed with status: {}", loginreponse.status());
    }
    let activities = get_activities(&client).await?;
    let activity_to_sign_up_for_pattern = "Klubaften, friroede 2024".to_string();
    if let Some(slot) = activities.into_iter().find(|slot| slot.name == activity_to_sign_up_for_pattern){
        let resp = sign_up_for_activity(&client, &slot).await?;
        if !resp.status().is_success() {
            println!("{:?}", resp.status());
        }
    }

    Ok(())
}

