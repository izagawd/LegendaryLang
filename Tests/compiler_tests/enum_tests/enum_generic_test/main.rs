fn main() -> i32 {
    let x = Option.Some(7);
    match x {
        Option.Some(val) => val,
        Option.None => 0
    }
}
