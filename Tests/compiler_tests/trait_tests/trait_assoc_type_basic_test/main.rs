trait Producer {
    let Output :! type;
    fn produce(input: Self) -> i32;
}

impl Producer for i32 {
    let Output :! type = bool;
    fn produce(input: i32) -> i32 {
        input
    }
}

fn main() -> i32 {
    (i32 as Producer).produce(42)
}
