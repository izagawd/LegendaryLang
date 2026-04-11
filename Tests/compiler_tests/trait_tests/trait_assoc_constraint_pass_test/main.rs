trait Producer: Sized {
    let Output :! Sized;
    fn produce(input: Self) -> i32;
}

impl Producer for i32 {
    let Output :! Sized = i32;
    fn produce(input: i32) -> i32 {
        input
    }
}

fn consume(T:! Sized +Producer(Output = i32), val: T) -> i32 {
    T.produce(val)
}

fn main() -> i32 {
    consume(i32, 42)
}
