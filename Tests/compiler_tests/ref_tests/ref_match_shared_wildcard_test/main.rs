enum Color {
    Red,
    Green,
    Blue
}

fn is_red(c: &Color) -> i32 {
    match c {
        Color.Red => 1,
        _ => 0
    }
}

fn main() -> i32 {
    let c = Color.Red;
    let d = Color.Blue;
    is_red(&c) + is_red(&d)
}
