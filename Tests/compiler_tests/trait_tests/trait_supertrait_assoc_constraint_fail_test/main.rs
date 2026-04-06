trait Producer {
    type Output;
}

trait IntProducer: Producer(Output = i32) {}

impl Producer for i32 {
    type Output = bool;
}

impl IntProducer for i32 {}

fn main() -> i32 {
    5
}
