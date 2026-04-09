fn maybe(x: i32) -> Option(i32) {
    if x > 0 {
        Option.Some(x)
    } else {
        Option.None
    }
}

fn main() -> i32 {
    let r1 = match maybe(5) {
        Option.Some(v) => v,
        Option.None => 0
    };
    let r2 = match maybe(0 - 3) {
        Option.Some(v) => v,
        Option.None => 0 - 1
    };
    r1 + r2
}
