trait Producer {
    type Output;
    fn produce(input: Self) -> i32;
}

impl Producer for i32 {
    type Output = bool;
    fn produce(input: i32) -> i32 {
        input
    }
}

fn main() -> i32 {
    <i32 as Producer>::produce(42)
}
