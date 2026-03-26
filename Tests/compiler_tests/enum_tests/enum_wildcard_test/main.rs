enum Color {
    Red,
    Green,
    Blue
}
fn main() -> i32 {
    let c = Color::Blue;
    match c {
        Color::Red => 1,
        _ => 99
    }
}
