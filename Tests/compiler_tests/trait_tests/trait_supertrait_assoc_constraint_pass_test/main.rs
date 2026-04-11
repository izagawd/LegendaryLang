trait Producer {
    let Output :! Sized;
}

trait IntProducer: Producer(Output = i32) {}

impl Producer for i32 {
    let Output :! Sized = i32;
}

impl IntProducer for i32 {}

fn main() -> i32 {
    42
}
