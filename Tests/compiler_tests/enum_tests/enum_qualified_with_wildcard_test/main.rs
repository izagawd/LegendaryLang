enum Color { Red, Green, Blue }

fn main() -> i32 {
    match Color.Green {
        Color.Red => 1,
        _ => 99
    }
}
