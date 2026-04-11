fn main() -> i32 {
    let arr: [i32; 3] = [10, 20, 42];
    match arr.get_ref(2) {
        Option.Some(v) => *v,
        Option.None => 0
    }
}
