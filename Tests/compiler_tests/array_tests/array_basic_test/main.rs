fn main() -> i32 {
    let arr = [10, 20, 30, 42];
    match arr.get_ref(3) {
        Option.Some(v) => *v,
        Option.None => 0
    }
}
