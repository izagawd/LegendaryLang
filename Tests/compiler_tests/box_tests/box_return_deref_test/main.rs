fn get_val(b: Box(i32)) -> i32 {
    *b
}

fn main() -> i32 {
    let a: i32 = get_val(Box.New(10));
    let b: i32 = get_val(Box.New(32));
    a + b
}
