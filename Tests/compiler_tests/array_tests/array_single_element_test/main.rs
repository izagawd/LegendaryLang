fn main() -> i32 {
    let arr = [42];
    match arr.get_ref(0) {
        Option.Some(v) => *v,
        Option.None => 0
    }
}
