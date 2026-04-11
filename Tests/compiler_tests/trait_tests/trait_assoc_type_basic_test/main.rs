trait Producer: Sized {
    let Output :! Sized;
    fn produce(input: Self) -> i32;
}

impl Producer for i32 {
    let Output :! Sized = bool;
    fn produce(input: i32) -> i32 {
        input
    }
}

fn main() -> i32 {
    (i32 as Producer).produce(42)
}
