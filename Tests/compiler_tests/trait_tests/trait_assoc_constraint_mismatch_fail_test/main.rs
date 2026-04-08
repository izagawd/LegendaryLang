trait Producer: Sized {
    let Output :! type;
    fn produce(input: Self) -> i32;
}

impl Producer for i32 {
    let Output :! type = bool;
    fn produce(input: i32) -> i32 {
        input
    }
}

fn consume(T:! Producer(Output = i32), val: T) -> i32 {
    T.produce(val)
}

fn main() -> i32 {
    consume(i32, 5)
}
