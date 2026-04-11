fn main() -> i32 {
    let arr = [10, 20, 30];
    match arr.get_mut(1) {
        Option.Some(v) => {
            *v = 42;
            0
        },
        Option.None => 0
    };
    match arr.get_ref(1) {
        Option.Some(v) => *v,
        Option.None => 0
    }
}
