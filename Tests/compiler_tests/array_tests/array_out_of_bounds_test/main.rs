fn main() -> i32 {
    let arr = [10, 20, 30];
    match arr.get_ref(5) {
        Option.Some(v) => *v,
        Option.None => 42
    }
}
