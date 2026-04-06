enum Color {
    Red,
    Green,
    Blue
}
fn main() -> i32 {
    let c = Color.Green;
    match c {
        Color.Red => 1,
        Color.Green => 2,
        Color.Blue => 3
    }
}
