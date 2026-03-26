struct NonCopy {
    val: i32
}

struct Holder {
    inner: NonCopy
}

impl Copy for Holder {}

fn main() -> i32 {
    5
}
