fn take_box(b: Box(i32)) -> i32 {
    *b
}

fn main() -> i32 {
    let b: Box(i32) = Box.New(77);
    take_box(b)
}
