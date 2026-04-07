fn main() -> i32 {
    let x = Option.Some(5);
    match x {
        Some(v) => v,
        None => 0
    }
}
